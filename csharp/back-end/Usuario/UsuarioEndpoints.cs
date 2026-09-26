// Rotas de usuario/identidade: cadastro, login, logout, me, recuperar-senha, perfil, admin.
using System.Text.Json;

namespace BancoCtf;

public static class UsuarioEndpoints
{
    public static void MapUsuario(this WebApplication app)
    {
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
                await Web.Responder(ctx, "Informe usuario e senha.", 400);
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

                await Web.Responder(ctx, "Cadastro realizado! id do usuario: " + usuarioId);
            }
            catch (Exception erro)
            {
                await Web.Responder(ctx, "Nao foi possivel cadastrar: " + erro.Message, 400);
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
                await Web.Responder(ctx, "Informe usuario e senha.", 400);
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
                    await Web.Responder(ctx, "Login OK! id do usuario: " + usuarioId);
                }
                else
                {
                    await Web.Responder(ctx, "Usuario ou senha invalidos.", 401);
                }
            }
            catch (Exception erro)
            {
                await Web.Responder(ctx, "Erro no login: " + erro.Message, 400);
            }
        });

        // ---------- Logout ----------
        app.MapPost("/logout", async (HttpContext ctx) =>
        {
            ctx.Session.Clear();
            await Web.Responder(ctx, "Logout feito.");
        });

        // ---------- Meu usuario (sessao) ----------
        app.MapGet("/me", async (HttpContext ctx) =>
        {
            var usuarioId = Web.UsuarioLogado(ctx);
            if (usuarioId == null)
            {
                await Web.Responder(ctx, "Faca login primeiro.", 401);
                return;
            }

            using var conexao = Banco.Conectar();

            var cmd = conexao.CreateCommand();
            cmd.CommandText = "SELECT usuario, admin FROM usuarios WHERE id = @id";
            cmd.Parameters.AddWithValue("@id", usuarioId);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                await Web.Responder(ctx, "Usuario nao encontrado.", 404);
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

            await Web.ResponderJson(ctx, new { id = usuarioId, usuario, admin, conta });
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
                await Web.Responder(ctx, "Informe usuario, cpf e a nova senha.", 400);
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
                    await Web.Responder(ctx, "Dados nao conferem.", 401);
                    return;
                }

                var usuarioId = Convert.ToInt64(achou);
                var atualizar = conexao.CreateCommand();
                atualizar.CommandText = "UPDATE usuarios SET senha = @s WHERE id = @id";
                atualizar.Parameters.AddWithValue("@s", novaSenha);
                atualizar.Parameters.AddWithValue("@id", usuarioId);
                atualizar.ExecuteNonQuery();

                await Web.Responder(ctx, "Senha atualizada.");
            }
            catch (Exception erro)
            {
                await Web.Responder(ctx, "Erro ao atualizar senha: " + erro.Message, 400);
            }
        });

        // ---------- Meu perfil (GET mostra, POST atualiza) ----------
        app.MapGet("/perfil", async (HttpContext ctx) =>
        {
            var usuarioId = Web.UsuarioLogado(ctx);
            if (usuarioId == null)
            {
                await Web.Responder(ctx, "Faca login primeiro.", 401);
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
                await Web.Responder(ctx, "Usuario nao encontrado.", 404);
                return;
            }

            await Web.ResponderJson(ctx, new
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
            var usuarioId = Web.UsuarioLogado(ctx);
            if (usuarioId == null)
            {
                await Web.Responder(ctx, "Faca login primeiro.", 401);
                return;
            }

            JsonDocument doc;
            try
            {
                doc = await JsonDocument.ParseAsync(ctx.Request.Body);
            }
            catch
            {
                await Web.Responder(ctx, "Envie os dados em JSON.", 400);
                return;
            }

            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                await Web.Responder(ctx, "Envie os dados em JSON.", 400);
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

                await Web.ResponderJson(ctx, new
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
                await Web.Responder(ctx, "Erro ao atualizar perfil: " + erro.Message, 400);
            }
        });

        // ---------- Admin: listar usuarios ----------
        app.MapGet("/admin/usuarios", async (HttpContext ctx) =>
        {
            var usuarioId = Web.UsuarioLogado(ctx);
            if (usuarioId == null)
            {
                await Web.Responder(ctx, "Faca login primeiro.", 401);
                return;
            }
            if (!Web.EhAdmin(usuarioId.Value))
            {
                await Web.Responder(ctx, "Acesso restrito a administradores.", 403);
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

            await Web.ResponderJson(ctx, usuarios);
        });

        // ---------- Admin: deletar usuario ----------
        app.MapPost("/admin/deletar", async (HttpContext ctx) =>
        {
            var usuarioId = Web.UsuarioLogado(ctx);
            if (usuarioId == null)
            {
                await Web.Responder(ctx, "Faca login primeiro.", 401);
                return;
            }
            if (!Web.EhAdmin(usuarioId.Value))
            {
                await Web.Responder(ctx, "Acesso restrito a administradores.", 403);
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
                await Web.Responder(ctx, "Informe o id do usuario.", 400);
                return;
            }

            if (string.IsNullOrEmpty(alvo))
            {
                await Web.Responder(ctx, "Informe o id do usuario.", 400);
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

                await Web.Responder(ctx, "Usuario removido.");
            }
            catch (Exception erro)
            {
                await Web.Responder(ctx, "Erro ao remover: " + erro.Message, 400);
            }
        });
    }
}
