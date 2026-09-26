// Ponto de entrada: cria o servidor, serve o front-end e registra as rotas.
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
Web.PastaComprovantes = Path.Combine(raiz, "comprovantes");
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

// Registra as rotas (organizadas em Sistema/ e Usuario/).
app.MapSistema();
app.MapUsuario();

Console.WriteLine("Servidor rodando em http://localhost:8092");
app.Run("http://localhost:8092");
