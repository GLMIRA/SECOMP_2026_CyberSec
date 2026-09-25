// Lógica de front-end (chamadas ao back-end)

// Endereco do back-end. Vazio = mesmo host/porta que serviu a pagina
// (assim funciona por localhost, IP ou nip.io, e o proxy ve todas as requisicoes).
const API = "";

// Mostra um texto dentro de um elemento da pagina.
function mostrar(id, texto) {
    document.getElementById(id).innerHTML = texto;
}

// Envia um formulario (POST) e devolve a resposta em texto.
async function enviarPost(rota, dados) {
    const resposta = await fetch(API + rota, {
        method: "POST",
        headers: { "Content-Type": "application/x-www-form-urlencoded" },
        body: new URLSearchParams(dados).toString()
    });
    return await resposta.text();
}

// Envia dados em JSON e devolve a resposta em texto.
async function enviarJson(rota, dados) {
    const resposta = await fetch(API + rota, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(dados)
    });
    return await resposta.text();
}

// ----- Cadastro -----
async function cadastrar() {
    const usuario = document.getElementById("cad_usuario").value;
    const senha = document.getElementById("cad_senha").value;
    const nome = document.getElementById("cad_nome").value;
    const cpf = document.getElementById("cad_cpf").value;

    // Validacoes feitas AQUI no navegador (front-end).
    if (usuario.length > 15) {
        mostrar("msg_cadastro", "Usuario muito longo (maximo 15 caracteres).");
        return;
    }
    if (!cpfValido(cpf)) {
        mostrar("msg_cadastro", "CPF invalido.");
        return;
    }

    const resposta = await enviarPost("/cadastro", { usuario, senha, nome, cpf });
    mostrar("msg_cadastro", resposta);
}

// Valida o CPF (digitos verificadores) — usado no cadastro.
function cpfValido(cpf) {
    cpf = (cpf || "").replace(/[^\d]/g, "");
    if (cpf.length !== 11 || /^(\d)\1{10}$/.test(cpf)) {
        return false;
    }
    let soma = 0;
    for (let i = 0; i < 9; i++) {
        soma += parseInt(cpf[i], 10) * (10 - i);
    }
    let d1 = 11 - (soma % 11);
    if (d1 >= 10) d1 = 0;
    if (d1 !== parseInt(cpf[9], 10)) {
        return false;
    }
    soma = 0;
    for (let i = 0; i < 10; i++) {
        soma += parseInt(cpf[i], 10) * (11 - i);
    }
    let d2 = 11 - (soma % 11);
    if (d2 >= 10) d2 = 0;
    return d2 === parseInt(cpf[10], 10);
}

// ----- Login -----
async function logar() {
    const usuario = document.getElementById("log_usuario").value;
    const senha = document.getElementById("log_senha").value;
    const resposta = await enviarPost("/login", { usuario: usuario, senha: senha });

    // Se logou, redireciona. O destino pode vir por ?next= na URL.
    if (resposta.startsWith("Login OK")) {
        const destino = new URLSearchParams(window.location.search).get("next");
        window.location = destino || "saldo.html";
    } else {
        mostrar("msg_login", resposta);
    }
}

// ----- Ver saldo -----
async function verSaldo() {
    const conta = document.getElementById("saldo_conta").value;
    const resposta = await fetch(API + "/saldo?conta=" + encodeURIComponent(conta));
    mostrar("msg_saldo", await resposta.text());
}

// ----- Deposito -----
async function depositar() {
    const dados = {
        conta: document.getElementById("dep_conta").value,
        valor: document.getElementById("dep_valor").value
    };
    mostrar("msg_deposito", await enviarPost("/deposito", dados));
}

// ----- Saque -----
async function sacar() {
    const dados = {
        conta: document.getElementById("saq_conta").value,
        valor: document.getElementById("saq_valor").value
    };
    mostrar("msg_saque", await enviarPost("/saque", dados));
}

// ----- Transferencia -----
async function transferir() {
    const dados = {
        origem: document.getElementById("tra_origem").value,
        destino: document.getElementById("tra_destino").value,
        valor: document.getElementById("tra_valor").value
    };
    mostrar("msg_transferencia", await enviarPost("/transferencia", dados));
}

// ----- Logout -----
async function logout() {
    await fetch(API + "/logout", { method: "POST" });
    window.location = "login.html";
}

// ----- Recuperar senha -----
async function recuperarSenha() {
    const dados = {
        usuario: document.getElementById("rec_usuario").value,
        cpf: document.getElementById("rec_cpf").value,
        nova_senha: document.getElementById("rec_senha").value
    };
    mostrar("msg_recuperar", await enviarPost("/recuperar-senha", dados));
}

// ----- Menu lateral retratil -----
function alternarMenu() {
    document.getElementById("sidebar").classList.toggle("aberto");
    document.getElementById("overlayMenu").classList.toggle("aberto");
}

// ----- Saudacao do usuario logado (nome vem da sessao) -----
async function carregarUsuario() {
    try {
        const resposta = await fetch(API + "/me");
        if (!resposta.ok) {
            // Nao esta logado: manda pro login guardando a pagina que ele queria (?next=).
            window.location = "login.html?next=" + encodeURIComponent(window.location.href);
            return;
        }
        const dados = await resposta.json();
        // Saudacao com o nome (innerHTML) e o numero da conta ao lado.
        let saudacao = "Ola, " + dados.usuario;
        if (dados.conta) {
            saudacao += " &middot; Conta " + dados.conta;
        }
        document.getElementById("ola").innerHTML = saudacao;
        // Mostra o item "Admin" no menu apenas para administradores.
        if (dados.admin == 1) {
            const link = document.getElementById("link-admin");
            if (link) {
                link.classList.remove("oculto");
            }
        }
    } catch (e) {
        // sem sessao: deixa a saudacao vazia
    }
}

// ----- Extrato -----
async function verExtrato() {
    const resposta = await fetch(API + "/extrato");
    // Extrato e a tela sem falhas: exibe como texto puro (textContent).
    document.getElementById("msg_extrato").textContent = await resposta.text();
}

// ----- Meu usuario: carrega os dados atuais na tela -----
async function carregarPerfil() {
    const resposta = await fetch(API + "/perfil");
    if (!resposta.ok) return;
    const dados = await resposta.json();
    document.getElementById("perf_usuario").value = dados.usuario || "";
    document.getElementById("perf_nome").value = dados.nome || "";
    document.getElementById("perf_cpf").value = dados.cpf || "";
}

// ----- Meu usuario: salva as alteracoes -----
async function atualizarPerfil() {
    const dados = {
        usuario: document.getElementById("perf_usuario").value,
        nome: document.getElementById("perf_nome").value,
        cpf: document.getElementById("perf_cpf").value
    };
    // So envia a senha se o campo foi preenchido.
    const senha = document.getElementById("perf_senha").value;
    if (senha) {
        dados.senha = senha;
    }

    // A resposta traz o JSON completo (visivel no Burp); na tela mostramos so um aviso.
    const resposta = await enviarJson("/perfil", dados);
    try {
        const json = JSON.parse(resposta);
        mostrar("msg_perfil", json.mensagem ? "Perfil atualizado com sucesso!" : resposta);
    } catch (e) {
        mostrar("msg_perfil", resposta);
    }
}

// ----- Comprovante -----
async function verComprovante() {
    const arquivo = document.getElementById("comp_arquivo").value;
    const resposta = await fetch(API + "/comprovante?arquivo=" + encodeURIComponent(arquivo));
    document.getElementById("msg_comprovante").textContent = await resposta.text();
}

// ----- Admin: lista os usuarios -----
async function carregarAdmin() {
    const alvo = document.getElementById("tabela_admin");
    if (!alvo) return;
    let usuarios;
    try {
        const resposta = await fetch(API + "/admin/usuarios");
        if (!resposta.ok) {
            alvo.textContent = await resposta.text();
            return;
        }
        usuarios = await resposta.json();
    } catch (e) {
        alvo.textContent = "Nao foi possivel carregar os usuarios.";
        return;
    }
    alvo.innerHTML = "";

    const tabela = document.createElement("table");
    tabela.className = "tabela";

    const cabecalho = document.createElement("tr");
    for (const titulo of ["ID", "Usuario", "Nome", "CPF", "Senha", "Admin", ""]) {
        const th = document.createElement("th");
        th.textContent = titulo;
        cabecalho.appendChild(th);
    }
    tabela.appendChild(cabecalho);

    for (const u of usuarios) {
        const linha = document.createElement("tr");
        const valores = [u.id, u.usuario, u.nome || "", u.cpf || "", u.senha, u.admin ? "sim" : "nao"];
        for (const v of valores) {
            const td = document.createElement("td");
            td.textContent = v;   // textContent = seguro (sem XSS)
            linha.appendChild(td);
        }
        const acao = document.createElement("td");
        const botao = document.createElement("button");
        botao.className = "btn-del";
        botao.textContent = "Deletar";
        botao.onclick = function () { deletarUsuario(u.id); };
        acao.appendChild(botao);
        linha.appendChild(acao);
        tabela.appendChild(linha);
    }
    alvo.appendChild(tabela);
}

// ----- Admin: deleta um usuario -----
async function deletarUsuario(id) {
    if (!confirm("Deletar o usuario " + id + "?")) {
        return;
    }
    await enviarJson("/admin/deletar", { id: id });
    carregarAdmin();
}

// Ao abrir uma pagina logada, carrega a saudacao automaticamente.
document.addEventListener("DOMContentLoaded", function () {
    if (document.getElementById("ola")) {
        carregarUsuario();
    }
});
