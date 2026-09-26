// Rotas do "sistema": saldo, deposito, saque, transferencia, extrato, comprovante.
using System.Globalization;

namespace BancoCtf;

public static class SistemaEndpoints
{
    public static void MapSistema(this WebApplication app)
    {
        // ---------- Ver saldo ----------
        app.MapGet("/saldo", async (HttpContext ctx) =>
        {
            if (Web.UsuarioLogado(ctx) == null)
            {
                await Web.Responder(ctx, "Faca login primeiro.", 401);
                return;
            }

            var conta = ctx.Request.Query["conta"].ToString();
            if (string.IsNullOrEmpty(conta))
            {
                await Web.Responder(ctx, "Informe a conta (?conta=...).", 400);
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
                    await Web.Responder(ctx, "Saldo da conta " + conta + ": " + saldo.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    await Web.Responder(ctx, "Conta nao encontrada.", 404);
                }
            }
            catch (Exception erro)
            {
                await Web.Responder(ctx, "Erro ao consultar saldo: " + erro.Message, 400);
            }
        });

        // ---------- Deposito ----------
        app.MapPost("/deposito", async (HttpContext ctx) =>
        {
            var usuarioId = Web.UsuarioLogado(ctx);
            if (usuarioId == null)
            {
                await Web.Responder(ctx, "Faca login primeiro.", 401);
                return;
            }

            var form = await ctx.Request.ReadFormAsync();
            var conta = form["conta"].ToString();
            var valorTexto = form["valor"].ToString();

            if (string.IsNullOrEmpty(conta) || string.IsNullOrEmpty(valorTexto))
            {
                await Web.Responder(ctx, "Informe a conta e o valor.", 400);
                return;
            }

            if (!double.TryParse(valorTexto, NumberStyles.Any, CultureInfo.InvariantCulture, out var valor))
            {
                await Web.Responder(ctx, "Valor invalido.", 400);
                return;
            }
            if (valor <= 0)
            {
                await Web.Responder(ctx, "O valor do deposito deve ser positivo.", 400);
                return;
            }
            // Nao pode depositar mais de 1.000.000,00 por operacao (nao ha teto de saldo).
            if (valor > 1_000_000.00)
            {
                await Web.Responder(ctx, "Deposito maximo por operacao e 1.000.000,00.", 400);
                return;
            }

            // So pode depositar em uma conta sua.
            if (!Banco.ContaPertence(conta, usuarioId.Value))
            {
                await Web.Responder(ctx, "Essa conta nao e sua.", 403);
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
                    await Web.Responder(ctx, "Conta nao encontrada.", 404);
                    return;
                }

                var novoSaldo = Convert.ToDouble(atual) + valor;

                // Grava o novo saldo (a conta pode ultrapassar 1.000.000).
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

                await Web.Responder(ctx, "Deposito realizado. Novo saldo: " + novoSaldo.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception erro)
            {
                await Web.Responder(ctx, "Erro no deposito: " + erro.Message, 400);
            }
        });

        // ---------- Saque ----------
        app.MapPost("/saque", async (HttpContext ctx) =>
        {
            var usuarioId = Web.UsuarioLogado(ctx);
            if (usuarioId == null)
            {
                await Web.Responder(ctx, "Faca login primeiro.", 401);
                return;
            }

            var form = await ctx.Request.ReadFormAsync();
            var conta = form["conta"].ToString();
            var valorTexto = form["valor"].ToString();

            if (string.IsNullOrEmpty(conta) || string.IsNullOrEmpty(valorTexto))
            {
                await Web.Responder(ctx, "Informe a conta e o valor.", 400);
                return;
            }

            if (!double.TryParse(valorTexto, NumberStyles.Any, CultureInfo.InvariantCulture, out var valor))
            {
                await Web.Responder(ctx, "Valor invalido.", 400);
                return;
            }
            if (valor <= 0)
            {
                await Web.Responder(ctx, "O valor do saque deve ser positivo.", 400);
                return;
            }
            // Nao pode sacar mais de 1.000.000,00 por operacao.
            if (valor > 1_000_000.00)
            {
                await Web.Responder(ctx, "Saque maximo por operacao e 1.000.000,00.", 400);
                return;
            }

            // So pode sacar de uma conta sua.
            if (!Banco.ContaPertence(conta, usuarioId.Value))
            {
                await Web.Responder(ctx, "Essa conta nao e sua.", 403);
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
                    await Web.Responder(ctx, "Conta nao encontrada.", 404);
                    return;
                }

                var saldoAtual = Convert.ToDouble(atual);
                if (valor > saldoAtual)
                {
                    await Web.Responder(ctx, "Saldo insuficiente.", 400);
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

                await Web.Responder(ctx, "Saque realizado. Novo saldo: " + novoSaldo.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception erro)
            {
                await Web.Responder(ctx, "Erro no saque: " + erro.Message, 400);
            }
        });

        // ---------- Transferencia ----------
        app.MapPost("/transferencia", async (HttpContext ctx) =>
        {
            var usuarioId = Web.UsuarioLogado(ctx);
            if (usuarioId == null)
            {
                await Web.Responder(ctx, "Faca login primeiro.", 401);
                return;
            }

            var form = await ctx.Request.ReadFormAsync();
            var origem = form["origem"].ToString();
            var destino = form["destino"].ToString();
            var valorTexto = form["valor"].ToString();

            if (string.IsNullOrEmpty(origem) || string.IsNullOrEmpty(destino) || string.IsNullOrEmpty(valorTexto))
            {
                await Web.Responder(ctx, "Informe origem, destino e valor.", 400);
                return;
            }
            if (origem == destino)
            {
                await Web.Responder(ctx, "Origem e destino nao podem ser a mesma conta.", 400);
                return;
            }

            if (!double.TryParse(valorTexto, NumberStyles.Any, CultureInfo.InvariantCulture, out var valor))
            {
                await Web.Responder(ctx, "Valor invalido.", 400);
                return;
            }
            if (valor > 1_000_000.00)
            {
                await Web.Responder(ctx, "Transferencia maxima por operacao e 1.000.000,00.", 400);
                return;
            }

            // So pode transferir de uma conta sua.
            if (!Banco.ContaPertence(origem, usuarioId.Value))
            {
                await Web.Responder(ctx, "Essa conta nao e sua.", 403);
                return;
            }

            try
            {
                using var conexao = Banco.Conectar();

                var lerOrigem = conexao.CreateCommand();
                lerOrigem.CommandText = "SELECT saldo FROM contas WHERE id = @c";
                lerOrigem.Parameters.AddWithValue("@c", origem);
                var saldoOrigemObj = lerOrigem.ExecuteScalar();
                if (saldoOrigemObj == null)
                {
                    await Web.Responder(ctx, "Conta de origem nao encontrada.", 404);
                    return;
                }

                var lerDestino = conexao.CreateCommand();
                lerDestino.CommandText = "SELECT saldo FROM contas WHERE id = @c";
                lerDestino.Parameters.AddWithValue("@c", destino);
                var saldoDestinoObj = lerDestino.ExecuteScalar();
                if (saldoDestinoObj == null)
                {
                    await Web.Responder(ctx, "Conta de destino nao encontrada.", 404);
                    return;
                }

                var saldoOrigem = Convert.ToDouble(saldoOrigemObj);
                var saldoDestino = Convert.ToDouble(saldoDestinoObj);

                if (valor > saldoOrigem)
                {
                    await Web.Responder(ctx, "Saldo insuficiente.", 400);
                    return;
                }

                var novoOrigem = saldoOrigem - valor;
                var novoDestino = saldoDestino + valor;

                var atualizarOrigem = conexao.CreateCommand();
                atualizarOrigem.CommandText = "UPDATE contas SET saldo = @s WHERE id = @c";
                atualizarOrigem.Parameters.AddWithValue("@s", novoOrigem);
                atualizarOrigem.Parameters.AddWithValue("@c", origem);
                atualizarOrigem.ExecuteNonQuery();

                var atualizarDestino = conexao.CreateCommand();
                atualizarDestino.CommandText = "UPDATE contas SET saldo = @s WHERE id = @c";
                atualizarDestino.Parameters.AddWithValue("@s", novoDestino);
                atualizarDestino.Parameters.AddWithValue("@c", destino);
                atualizarDestino.ExecuteNonQuery();

                var registrar = conexao.CreateCommand();
                registrar.CommandText = "INSERT INTO transferencias (conta_origem, conta_destino, valor) VALUES (@o, @d, @v)";
                registrar.Parameters.AddWithValue("@o", origem);
                registrar.Parameters.AddWithValue("@d", destino);
                registrar.Parameters.AddWithValue("@v", valor);
                registrar.ExecuteNonQuery();

                var movSaida = conexao.CreateCommand();
                movSaida.CommandText = "INSERT INTO movimentacoes (conta_id, tipo, valor) VALUES (@c, 'transferencia_saida', @v)";
                movSaida.Parameters.AddWithValue("@c", origem);
                movSaida.Parameters.AddWithValue("@v", valor);
                movSaida.ExecuteNonQuery();

                var movEntrada = conexao.CreateCommand();
                movEntrada.CommandText = "INSERT INTO movimentacoes (conta_id, tipo, valor) VALUES (@c, 'transferencia_entrada', @v)";
                movEntrada.Parameters.AddWithValue("@c", destino);
                movEntrada.Parameters.AddWithValue("@v", valor);
                movEntrada.ExecuteNonQuery();

                await Web.Responder(ctx, "Transferencia realizada. Novo saldo da origem: " + novoOrigem.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception erro)
            {
                await Web.Responder(ctx, "Erro na transferencia: " + erro.Message, 400);
            }
        });

        // ---------- Extrato ----------
        app.MapGet("/extrato", async (HttpContext ctx) =>
        {
            var usuarioId = Web.UsuarioLogado(ctx);
            if (usuarioId == null)
            {
                await Web.Responder(ctx, "Faca login primeiro.", 401);
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

                await Web.Responder(ctx, linhas.Count == 0 ? "Nenhuma movimentacao." : string.Join("\n", linhas));
            }
            catch (Exception erro)
            {
                await Web.Responder(ctx, "Erro ao obter extrato: " + erro.Message, 400);
            }
        });

        // ---------- Comprovante ----------
        app.MapGet("/comprovante", async (HttpContext ctx) =>
        {
            if (Web.UsuarioLogado(ctx) == null)
            {
                await Web.Responder(ctx, "Faca login primeiro.", 401);
                return;
            }

            var arquivo = ctx.Request.Query["arquivo"].ToString();
            if (string.IsNullOrEmpty(arquivo))
            {
                await Web.Responder(ctx, "Informe o arquivo (?arquivo=...).", 400);
                return;
            }

            try
            {
                var caminho = Path.Combine(Web.PastaComprovantes, arquivo);
                var conteudo = await File.ReadAllTextAsync(caminho);
                await Web.Responder(ctx, conteudo);
            }
            catch (Exception erro)
            {
                await Web.Responder(ctx, "Erro ao abrir comprovante: " + erro.Message, 400);
            }
        });
    }
}
