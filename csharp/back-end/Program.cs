// Ponto de entrada: cria o servidor, serve o front-end e registra as rotas.
using System.Globalization;
using System.Text.Json;
using BancoCtf;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Sessao (cookie). A chave de protecao e fixa (material educacional).
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "session";
    options.Cookie.HttpOnly = true;
});

var app = builder.Build();

// Caminhos do projeto (a pasta "back-end" fica dentro da raiz do projeto).
var raiz = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, ".."));
var pastaFront = Path.Combine(raiz, "front-end");
var pastaComprovantes = Path.Combine(raiz, "comprovantes");
Banco.CaminhoBanco = Path.Combine(raiz, "db", "banco.db");
Banco.CaminhoSchema = Path.Combine(raiz, "db", "schema.sql");

// Cria o banco na primeira execucao.
Banco.CriarSeNaoExiste();

// Libera o acesso a partir do front-end (localhost, material educacional).
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["Access-Control-Allow-Origin"] = "*";
    await next();
});

// Serve o front-end ("/" abre o index.html).
var provider = new PhysicalFileProvider(pastaFront);
app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = provider });
app.UseStaticFiles(new StaticFileOptions { FileProvider = provider });

app.UseSession();

// ---------- Helpers ----------
long? UsuarioLogado(HttpContext ctx) => ctx.Session.GetInt32("usuario_id");

async Task Responder(HttpContext ctx, string texto, int codigo = 200)
{
    ctx.Response.StatusCode = codigo;
    ctx.Response.ContentType = "text/plain; charset=utf-8";
    await ctx.Response.WriteAsync(texto);
}

async Task ResponderJson(HttpContext ctx, object dados, int codigo = 200)
{
    ctx.Response.StatusCode = codigo;
    ctx.Response.ContentType = "application/json; charset=utf-8";
    await ctx.Response.WriteAsync(JsonSerializer.Serialize(dados));
}

bool EhAdmin(long usuarioId)
{
    using var conexao = Banco.Conectar();
    using var cmd = conexao.CreateCommand();
    cmd.CommandText = "SELECT admin FROM usuarios WHERE id = @id";
    cmd.Parameters.AddWithValue("@id", usuarioId);
    var r = cmd.ExecuteScalar();
    return r != null && Convert.ToInt64(r) == 1;
}

// ---------- Cadastro ----------
app.MapPost("/cadastro", async (HttpContext ctx) =>
{
    var form = await ctx.Request.ReadFormAsync();
    var usuario = form["usuario"].ToString();
    var senha = form["senha"].ToString();
    var nome = form["nome"].ToString();
    var cpf = form["cpf"].ToString();

    if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(senha))
    {
        await Responder(ctx, "Informe usuario e senha.", 400);
        return;
    }

    try
    {
        using var conexao = Banco.Conectar();

        var inserirUsuario = conexao.CreateCommand();
        inserirUsuario.CommandText = "INSERT INTO usuarios (usuario, senha, admin) VALUES (@u, @s, 0)";
        inserirUsuario.Parameters.AddWithValue("@u", usuario);
        inserirUsuario.Parameters.AddWithValue("@s", senha);
        inserirUsuario.ExecuteNonQuery();

        var pegarId = conexao.CreateCommand();
        pegarId.CommandText = "SELECT last_insert_rowid()";
        var usuarioId = (long)pegarId.ExecuteScalar();

        var inserirPerfil = conexao.CreateCommand();
        inserirPerfil.CommandText = "INSERT INTO perfis (usuario_id, nome, cpf) VALUES (@id, @n, @c)";
        inserirPerfil.Parameters.AddWithValue("@id", usuarioId);
        inserirPerfil.Parameters.AddWithValue("@n", nome);
        inserirPerfil.Parameters.AddWithValue("@c", cpf);
        inserirPerfil.ExecuteNonQuery();

        var inserirConta = conexao.CreateCommand();
        inserirConta.CommandText = "INSERT INTO contas (usuario_id, saldo) VALUES (@id, 0)";
        inserirConta.Parameters.AddWithValue("@id", usuarioId);
        inserirConta.ExecuteNonQuery();

        await Responder(ctx, "Cadastro realizado! id do usuario: " + usuarioId);
    }
    catch (Exception erro)
    {
        await Responder(ctx, "Nao foi possivel cadastrar: " + erro.Message, 400);
    }
});

// ---------- Login ----------
app.MapPost("/login", async (HttpContext ctx) =>
{
    var form = await ctx.Request.ReadFormAsync();
    var usuario = form["usuario"].ToString();
    var senha = form["senha"].ToString();

    if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(senha))
    {
        await Responder(ctx, "Informe usuario e senha.", 400);
        return;
    }

    try
    {
        using var conexao = Banco.Conectar();
        using var cmd = conexao.CreateCommand();
        // Monta a consulta com o usuario e a senha recebidos.
        cmd.CommandText = "SELECT id FROM usuarios "
            + "WHERE usuario = '" + usuario + "' AND senha = '" + senha + "'";
        using var reader = cmd.ExecuteReader();

        if (reader.Read())
        {
            var usuarioId = reader.GetInt32(0);
            ctx.Session.SetInt32("usuario_id", usuarioId);
            await Responder(ctx, "Login OK! id do usuario: " + usuarioId);
        }
        else
        {
            await Responder(ctx, "Usuario ou senha invalidos.", 401);
        }
    }
    catch (Exception erro)
    {
        await Responder(ctx, "Erro no login: " + erro.Message, 400);
    }
});

// ---------- Logout ----------
app.MapPost("/logout", async (HttpContext ctx) =>
{
    ctx.Session.Clear();
    await Responder(ctx, "Logout feito.");
});

// ---------- Meu usuario (sessao) ----------
app.MapGet("/me", async (HttpContext ctx) =>
{
    var usuarioId = UsuarioLogado(ctx);
    if (usuarioId == null)
    {
        await Responder(ctx, "Faca login primeiro.", 401);
        return;
    }

    using var conexao = Banco.Conectar();

    var cmd = conexao.CreateCommand();
    cmd.CommandText = "SELECT usuario, admin FROM usuarios WHERE id = @id";
    cmd.Parameters.AddWithValue("@id", usuarioId);
    using var reader = cmd.ExecuteReader();
    if (!reader.Read())
    {
        await Responder(ctx, "Usuario nao encontrado.", 404);
        return;
    }
    var usuario = reader.GetString(0);
    var admin = reader.GetInt64(1);
    reader.Close();

    var cmdConta = conexao.CreateCommand();
    cmdConta.CommandText = "SELECT id FROM contas WHERE usuario_id = @id ORDER BY id LIMIT 1";
    cmdConta.Parameters.AddWithValue("@id", usuarioId);
    var contaObj = cmdConta.ExecuteScalar();
    long? conta = contaObj == null ? null : Convert.ToInt64(contaObj);

    await ResponderJson(ctx, new { id = usuarioId, usuario, admin, conta });
});

// ---------- Recuperar senha ----------
app.MapPost("/recuperar-senha", async (HttpContext ctx) =>
{
    var form = await ctx.Request.ReadFormAsync();
    var usuario = form["usuario"].ToString();
    var cpf = form["cpf"].ToString();
    var novaSenha = form["nova_senha"].ToString();

    if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(cpf) || string.IsNullOrEmpty(novaSenha))
    {
        await Responder(ctx, "Informe usuario, cpf e a nova senha.", 400);
        return;
    }

    try
    {
        using var conexao = Banco.Conectar();
        using var cmd = conexao.CreateCommand();
        // Confere a identidade: o usuario e o cpf precisam bater.
        cmd.CommandText = "SELECT u.id FROM usuarios u "
            + "JOIN perfis p ON u.id = p.usuario_id "
            + "WHERE u.usuario = '" + usuario + "' AND p.cpf = '" + cpf + "'";
        var achou = cmd.ExecuteScalar();

        if (achou == null)
        {
            await Responder(ctx, "Dados nao conferem.", 401);
            return;
        }

        var usuarioId = Convert.ToInt64(achou);
        var atualizar = conexao.CreateCommand();
        atualizar.CommandText = "UPDATE usuarios SET senha = @s WHERE id = @id";
        atualizar.Parameters.AddWithValue("@s", novaSenha);
        atualizar.Parameters.AddWithValue("@id", usuarioId);
        atualizar.ExecuteNonQuery();

        await Responder(ctx, "Senha atualizada.");
    }
    catch (Exception erro)
    {
        await Responder(ctx, "Erro ao atualizar senha: " + erro.Message, 400);
    }
});

// ---------- Ver saldo ----------
app.MapGet("/saldo", async (HttpContext ctx) =>
{
    if (UsuarioLogado(ctx) == null)
    {
        await Responder(ctx, "Faca login primeiro.", 401);
        return;
    }

    var conta = ctx.Request.Query["conta"].ToString();
    if (string.IsNullOrEmpty(conta))
    {
        await Responder(ctx, "Informe a conta (?conta=...).", 400);
        return;
    }

    try
    {
        using var conexao = Banco.Conectar();
        using var cmd = conexao.CreateCommand();
        cmd.CommandText = "SELECT saldo FROM contas WHERE id = @conta";
        cmd.Parameters.AddWithValue("@conta", conta);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            var saldo = reader.GetDouble(0);
            await Responder(ctx, "Saldo da conta " + conta + ": " + saldo.ToString(CultureInfo.InvariantCulture));
        }
        else
        {
            await Responder(ctx, "Conta nao encontrada.", 404);
        }
    }
    catch (Exception erro)
    {
        await Responder(ctx, "Erro ao consultar saldo: " + erro.Message, 400);
    }
});

// ---------- Deposito ----------
app.MapPost("/deposito", async (HttpContext ctx) =>
{
    var usuarioId = UsuarioLogado(ctx);
    if (usuarioId == null)
    {
        await Responder(ctx, "Faca login primeiro.", 401);
        return;
    }

    var form = await ctx.Request.ReadFormAsync();
    var conta = form["conta"].ToString();
    var valorTexto = form["valor"].ToString();

    if (string.IsNullOrEmpty(conta) || string.IsNullOrEmpty(valorTexto))
    {
        await Responder(ctx, "Informe a conta e o valor.", 400);
        return;
    }

    if (!double.TryParse(valorTexto, NumberStyles.Any, CultureInfo.InvariantCulture, out var valor))
    {
        await Responder(ctx, "Valor invalido.", 400);
        return;
    }
    if (valor <= 0)
    {
        await Responder(ctx, "O valor do deposito deve ser positivo.", 400);
        return;
    }

    // So pode depositar em uma conta sua.
    if (!Banco.ContaPertence(conta, usuarioId.Value))
    {
        await Responder(ctx, "Essa conta nao e sua.", 403);
        return;
    }

    try
    {
        using var conexao = Banco.Conectar();

        var ler = conexao.CreateCommand();
        ler.CommandText = "SELECT saldo FROM contas WHERE id = @conta";
        ler.Parameters.AddWithValue("@conta", conta);
        var atual = ler.ExecuteScalar();
        if (atual == null)
        {
            await Responder(ctx, "Conta nao encontrada.", 404);
            return;
        }

        var novoSaldo = Convert.ToDouble(atual) + valor;
        // Aplica a regra do limite maximo por conta.
        if (novoSaldo > 1_000_000.00)
        {
            await Responder(ctx, "Limite de 1.000.000,00 por conta excedido.", 400);
            return;
        }

        var atualizar = conexao.CreateCommand();
        atualizar.CommandText = "UPDATE contas SET saldo = @s WHERE id = @conta";
        atualizar.Parameters.AddWithValue("@s", novoSaldo);
        atualizar.Parameters.AddWithValue("@conta", conta);
        atualizar.ExecuteNonQuery();

        var mov = conexao.CreateCommand();
        mov.CommandText = "INSERT INTO movimentacoes (conta_id, tipo, valor) VALUES (@c, 'deposito', @v)";
        mov.Parameters.AddWithValue("@c", conta);
        mov.Parameters.AddWithValue("@v", valor);
        mov.ExecuteNonQuery();

        await Responder(ctx, "Deposito realizado. Novo saldo: " + novoSaldo.ToString(CultureInfo.InvariantCulture));
    }
    catch (Exception erro)
    {
        await Responder(ctx, "Erro no deposito: " + erro.Message, 400);
    }
});

// ---------- Saque ----------
app.MapPost("/saque", async (HttpContext ctx) =>
{
    var usuarioId = UsuarioLogado(ctx);
    if (usuarioId == null)
    {
        await Responder(ctx, "Faca login primeiro.", 401);
        return;
    }

    var form = await ctx.Request.ReadFormAsync();
    var conta = form["conta"].ToString();
    var valorTexto = form["valor"].ToString();

    if (string.IsNullOrEmpty(conta) || string.IsNullOrEmpty(valorTexto))
    {
        await Responder(ctx, "Informe a conta e o valor.", 400);
        return;
    }

    if (!double.TryParse(valorTexto, NumberStyles.Any, CultureInfo.InvariantCulture, out var valor))
    {
        await Responder(ctx, "Valor invalido.", 400);
        return;
    }
    if (valor <= 0)
    {
        await Responder(ctx, "O valor do saque deve ser positivo.", 400);
        return;
    }

    // So pode sacar de uma conta sua.
    if (!Banco.ContaPertence(conta, usuarioId.Value))
    {
        await Responder(ctx, "Essa conta nao e sua.", 403);
        return;
    }

    try
    {
        using var conexao = Banco.Conectar();

        var ler = conexao.CreateCommand();
        ler.CommandText = "SELECT saldo FROM contas WHERE id = @conta";
        ler.Parameters.AddWithValue("@conta", conta);
        var atual = ler.ExecuteScalar();
        if (atual == null)
        {
            await Responder(ctx, "Conta nao encontrada.", 404);
            return;
        }

        var saldoAtual = Convert.ToDouble(atual);
        if (valor > saldoAtual)
        {
            await Responder(ctx, "Saldo insuficiente.", 400);
            return;
        }

        var novoSaldo = saldoAtual - valor;

        var atualizar = conexao.CreateCommand();
        atualizar.CommandText = "UPDATE contas SET saldo = @s WHERE id = @conta";
        atualizar.Parameters.AddWithValue("@s", novoSaldo);
        atualizar.Parameters.AddWithValue("@conta", conta);
        atualizar.ExecuteNonQuery();

        var mov = conexao.CreateCommand();
        mov.CommandText = "INSERT INTO movimentacoes (conta_id, tipo, valor) VALUES (@c, 'saque', @v)";
        mov.Parameters.AddWithValue("@c", conta);
        mov.Parameters.AddWithValue("@v", valor);
        mov.ExecuteNonQuery();

        await Responder(ctx, "Saque realizado. Novo saldo: " + novoSaldo.ToString(CultureInfo.InvariantCulture));
    }
    catch (Exception erro)
    {
        await Responder(ctx, "Erro no saque: " + erro.Message, 400);
    }
});

// ---------- Transferencia (ESQUELETO) ----------
app.MapPost("/transferencia", async (HttpContext ctx) =>
{
    var usuarioId = UsuarioLogado(ctx);
    if (usuarioId == null)
    {
        await Responder(ctx, "Faca login primeiro.", 401);
        return;
    }

    var form = await ctx.Request.ReadFormAsync();
    var origem = form["origem"].ToString();
    var destino = form["destino"].ToString();
    var valor = form["valor"].ToString();

    // TODO: validar os campos recebidos (origem, destino e valor).
    // TODO: conferir que a conta de origem pertence ao usuario logado (Banco.ContaPertence).
    // TODO: buscar o saldo da origem e aplicar as regras de negocio
    //       (valor positivo, saldo suficiente, teto de 1.000.000,00).
    // TODO: debitar da origem e creditar no destino.
    // TODO: registrar em "transferencias" e em "movimentacoes" (pra aparecer no extrato).
    // TODO: responder com o resultado da operacao.
    await Responder(ctx, "Transferencia ainda nao implementada.", 501);
});

// ---------- Extrato ----------
app.MapGet("/extrato", async (HttpContext ctx) =>
{
    var usuarioId = UsuarioLogado(ctx);
    if (usuarioId == null)
    {
        await Responder(ctx, "Faca login primeiro.", 401);
        return;
    }

    try
    {
        using var conexao = Banco.Conectar();
        using var cmd = conexao.CreateCommand();
        cmd.CommandText = "SELECT tipo, conta_id, valor, data FROM movimentacoes "
            + "WHERE conta_id IN (SELECT id FROM contas WHERE usuario_id = @id) "
            + "ORDER BY data DESC";
        cmd.Parameters.AddWithValue("@id", usuarioId);
        using var reader = cmd.ExecuteReader();

        var linhas = new List<string>();
        while (reader.Read())
        {
            var tipo = reader.GetString(0);
            var conta = reader.GetInt64(1);
            var valor = reader.GetDouble(2);
            var data = reader.GetString(3);
            linhas.Add(tipo.ToUpper() + "  conta " + conta + "  R$ "
                + valor.ToString(CultureInfo.InvariantCulture) + "  (" + data + ")");
        }

        await Responder(ctx, linhas.Count == 0 ? "Nenhuma movimentacao." : string.Join("\n", linhas));
    }
    catch (Exception erro)
    {
        await Responder(ctx, "Erro ao obter extrato: " + erro.Message, 400);
    }
});

// ---------- Meu perfil (GET mostra, POST atualiza) ----------
app.MapGet("/perfil", async (HttpContext ctx) =>
{
    var usuarioId = UsuarioLogado(ctx);
    if (usuarioId == null)
    {
        await Responder(ctx, "Faca login primeiro.", 401);
        return;
    }

    using var conexao = Banco.Conectar();
    using var cmd = conexao.CreateCommand();
    cmd.CommandText = "SELECT u.usuario, u.admin, p.nome, p.cpf "
        + "FROM usuarios u JOIN perfis p ON u.id = p.usuario_id WHERE u.id = @id";
    cmd.Parameters.AddWithValue("@id", usuarioId);
    using var reader = cmd.ExecuteReader();
    if (!reader.Read())
    {
        await Responder(ctx, "Usuario nao encontrado.", 404);
        return;
    }

    await ResponderJson(ctx, new
    {
        id = usuarioId,
        usuario = reader.GetString(0),
        admin = reader.GetInt64(1),
        nome = reader.IsDBNull(2) ? null : reader.GetString(2),
        cpf = reader.IsDBNull(3) ? null : reader.GetString(3),
    });
});

app.MapPost("/perfil", async (HttpContext ctx) =>
{
    var usuarioId = UsuarioLogado(ctx);
    if (usuarioId == null)
    {
        await Responder(ctx, "Faca login primeiro.", 401);
        return;
    }

    JsonDocument doc;
    try
    {
        doc = await JsonDocument.ParseAsync(ctx.Request.Body);
    }
    catch
    {
        await Responder(ctx, "Envie os dados em JSON.", 400);
        return;
    }

    var root = doc.RootElement;
    if (root.ValueKind != JsonValueKind.Object)
    {
        await Responder(ctx, "Envie os dados em JSON.", 400);
        return;
    }

    try
    {
        using var conexao = Banco.Conectar();

        // Monta o UPDATE com os campos recebidos.
        var camposUsuario = new List<KeyValuePair<string, string>>();
        foreach (var prop in root.EnumerateObject())
        {
            var valor = prop.Value.ValueKind == JsonValueKind.String
                ? prop.Value.GetString()
                : prop.Value.GetRawText();

            if (prop.Name == "nome")
            {
                var c = conexao.CreateCommand();
                c.CommandText = "UPDATE perfis SET nome = @v WHERE usuario_id = @id";
                c.Parameters.AddWithValue("@v", valor);
                c.Parameters.AddWithValue("@id", usuarioId);
                c.ExecuteNonQuery();
            }
            else if (prop.Name == "cpf")
            {
                var c = conexao.CreateCommand();
                c.CommandText = "UPDATE perfis SET cpf = @v WHERE usuario_id = @id";
                c.Parameters.AddWithValue("@v", valor);
                c.Parameters.AddWithValue("@id", usuarioId);
                c.ExecuteNonQuery();
            }
            else
            {
                camposUsuario.Add(new KeyValuePair<string, string>(prop.Name, valor));
            }
        }

        if (camposUsuario.Count > 0)
        {
            var partes = new List<string>();
            var cmd = conexao.CreateCommand();
            for (var i = 0; i < camposUsuario.Count; i++)
            {
                partes.Add(camposUsuario[i].Key + " = @p" + i);
                cmd.Parameters.AddWithValue("@p" + i, camposUsuario[i].Value);
            }
            cmd.Parameters.AddWithValue("@id", usuarioId);
            cmd.CommandText = "UPDATE usuarios SET " + string.Join(", ", partes) + " WHERE id = @id";
            cmd.ExecuteNonQuery();
        }

        // Devolve o perfil completo ja atualizado (mais visual na resposta).
        var ler = conexao.CreateCommand();
        ler.CommandText = "SELECT u.usuario, u.admin, p.nome, p.cpf "
            + "FROM usuarios u JOIN perfis p ON u.id = p.usuario_id WHERE u.id = @id";
        ler.Parameters.AddWithValue("@id", usuarioId);
        using var reader = ler.ExecuteReader();
        reader.Read();

        await ResponderJson(ctx, new
        {
            mensagem = "Perfil atualizado.",
            id = usuarioId,
            usuario = reader.GetString(0),
            admin = reader.GetInt64(1),
            nome = reader.IsDBNull(2) ? null : reader.GetString(2),
            cpf = reader.IsDBNull(3) ? null : reader.GetString(3),
        });
    }
    catch (Exception erro)
    {
        await Responder(ctx, "Erro ao atualizar perfil: " + erro.Message, 400);
    }
});

// ---------- Comprovante ----------
app.MapGet("/comprovante", async (HttpContext ctx) =>
{
    if (UsuarioLogado(ctx) == null)
    {
        await Responder(ctx, "Faca login primeiro.", 401);
        return;
    }

    var arquivo = ctx.Request.Query["arquivo"].ToString();
    if (string.IsNullOrEmpty(arquivo))
    {
        await Responder(ctx, "Informe o arquivo (?arquivo=...).", 400);
        return;
    }

    try
    {
        var caminho = Path.Combine(pastaComprovantes, arquivo);
        var conteudo = await File.ReadAllTextAsync(caminho);
        await Responder(ctx, conteudo);
    }
    catch (Exception erro)
    {
        await Responder(ctx, "Erro ao abrir comprovante: " + erro.Message, 400);
    }
});

// ---------- Admin: listar usuarios ----------
app.MapGet("/admin/usuarios", async (HttpContext ctx) =>
{
    var usuarioId = UsuarioLogado(ctx);
    if (usuarioId == null)
    {
        await Responder(ctx, "Faca login primeiro.", 401);
        return;
    }
    if (!EhAdmin(usuarioId.Value))
    {
        await Responder(ctx, "Acesso restrito a administradores.", 403);
        return;
    }

    using var conexao = Banco.Conectar();
    using var cmd = conexao.CreateCommand();
    cmd.CommandText = "SELECT u.id, u.usuario, u.senha, u.admin, p.nome, p.cpf "
        + "FROM usuarios u LEFT JOIN perfis p ON u.id = p.usuario_id ORDER BY u.id";
    using var reader = cmd.ExecuteReader();

    var usuarios = new List<object>();
    while (reader.Read())
    {
        usuarios.Add(new
        {
            id = reader.GetInt64(0),
            usuario = reader.GetString(1),
            senha = reader.GetString(2),
            admin = reader.GetInt64(3),
            nome = reader.IsDBNull(4) ? null : reader.GetString(4),
            cpf = reader.IsDBNull(5) ? null : reader.GetString(5),
        });
    }

    await ResponderJson(ctx, usuarios);
});

// ---------- Admin: deletar usuario ----------
app.MapPost("/admin/deletar", async (HttpContext ctx) =>
{
    var usuarioId = UsuarioLogado(ctx);
    if (usuarioId == null)
    {
        await Responder(ctx, "Faca login primeiro.", 401);
        return;
    }
    if (!EhAdmin(usuarioId.Value))
    {
        await Responder(ctx, "Acesso restrito a administradores.", 403);
        return;
    }

    string alvo;
    try
    {
        using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
        alvo = doc.RootElement.GetProperty("id").ToString();
    }
    catch
    {
        await Responder(ctx, "Informe o id do usuario.", 400);
        return;
    }

    if (string.IsNullOrEmpty(alvo))
    {
        await Responder(ctx, "Informe o id do usuario.", 400);
        return;
    }

    try
    {
        using var conexao = Banco.Conectar();
        // Remove os dados ligados ao usuario antes de apaga-lo.
        var passos = new[]
        {
            "DELETE FROM movimentacoes WHERE conta_id IN (SELECT id FROM contas WHERE usuario_id = @id)",
            "DELETE FROM transferencias WHERE conta_origem IN (SELECT id FROM contas WHERE usuario_id = @id) OR conta_destino IN (SELECT id FROM contas WHERE usuario_id = @id)",
            "DELETE FROM contas WHERE usuario_id = @id",
            "DELETE FROM perfis WHERE usuario_id = @id",
            "DELETE FROM usuarios WHERE id = @id",
        };
        foreach (var sql in passos)
        {
            var c = conexao.CreateCommand();
            c.CommandText = sql;
            c.Parameters.AddWithValue("@id", alvo);
            c.ExecuteNonQuery();
        }

        await Responder(ctx, "Usuario removido.");
    }
    catch (Exception erro)
    {
        await Responder(ctx, "Erro ao remover: " + erro.Message, 400);
    }
});

Console.WriteLine("Servidor rodando em http://localhost:8092");
app.Run("http://localhost:8092");
