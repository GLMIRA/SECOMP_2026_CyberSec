// Front-end: chamadas ao back-end.

const API = "";

function mostrar(id, texto) {
    document.getElementById(id).innerHTML = texto;
}

async function enviarPost(rota, dados) {
    const resposta = await fetch(API + rota, {
        method: "POST",
        headers: { "Content-Type": "application/x-www-form-urlencoded" },
        body: new URLSearchParams(dados).toString()
    });
    return await resposta.text();
}

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
    const valor = parseFloat(dados.valor);
    if (isNaN(valor) || valor <= 0) {
        mostrar("msg_transferencia", "O valor da transferencia deve ser positivo.");
        return;
    }
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

// ----- Menu -----
function alternarMenu() {
    document.getElementById("sidebar").classList.toggle("aberto");
    document.getElementById("overlayMenu").classList.toggle("aberto");
}

// ----- Saudacao -----
async function carregarUsuario() {
    try {
        const resposta = await fetch(API + "/me");
        if (!resposta.ok) {
            window.location = "login.html?next=" + encodeURIComponent(window.location.href);
            return;
        }
        const dados = await resposta.json();
        let saudacao = "Ola, " + dados.usuario;
        if (dados.conta) {
            saudacao += " &middot; Conta " + dados.conta;
        }
        document.getElementById("ola").innerHTML = saudacao;
        if (dados.admin == 1) {
            const link = document.getElementById("link-admin");
            if (link) {
                link.classList.remove("oculto");
            }
        }
    } catch (e) {
    }
}

// ----- Extrato -----
async function verExtrato() {
    const resposta = await fetch(API + "/extrato");
    document.getElementById("msg_extrato").textContent = await resposta.text();
}

// ----- Meu usuario -----
async function carregarPerfil() {
    const resposta = await fetch(API + "/perfil");
    if (!resposta.ok) return;
    const dados = await resposta.json();
    document.getElementById("perf_usuario").value = dados.usuario || "";
    document.getElementById("perf_nome").value = dados.nome || "";
    document.getElementById("perf_cpf").value = dados.cpf || "";
}

async function atualizarPerfil() {
    const dados = {
        usuario: document.getElementById("perf_usuario").value,
        nome: document.getElementById("perf_nome").value,
        cpf: document.getElementById("perf_cpf").value
    };
    const senha = document.getElementById("perf_senha").value;
    if (senha) {
        dados.senha = senha;
    }

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

// ----- Admin: usuarios -----
function iniciais(nome) {
    if (!nome) return "?";
    const partes = String(nome).trim().split(" ").filter(Boolean);
    if (partes.length === 1) return partes[0].substring(0, 2).toUpperCase();
    return (partes[0][0] + partes[partes.length - 1][0]).toUpperCase();
}

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
    const contador = document.getElementById("user-count");
    if (contador) contador.textContent = usuarios.length;

    alvo.innerHTML = "";
    const tabela = document.createElement("table");

    const thead = document.createElement("thead");
    const cabecalho = document.createElement("tr");
    for (const titulo of ["ID", "Usuario", "CPF", "Admin", ""]) {
        const th = document.createElement("th");
        th.textContent = titulo;
        cabecalho.appendChild(th);
    }
    thead.appendChild(cabecalho);
    tabela.appendChild(thead);

    const tbody = document.createElement("tbody");
    for (const u of usuarios) {
        const linha = document.createElement("tr");

        const tdId = document.createElement("td");
        tdId.textContent = String(u.id).padStart(2, "0");
        linha.appendChild(tdId);

        // Celula do usuario (avatar + nome + @usuario). textContent = sem XSS aqui.
        const tdUser = document.createElement("td");
        const cell = document.createElement("div");
        cell.className = "user-cell";
        const avatar = document.createElement("div");
        avatar.className = "user-avatar";
        avatar.textContent = iniciais(u.nome || u.usuario);
        const info = document.createElement("div");
        info.className = "user-info";
        const nomeRow = document.createElement("div");
        nomeRow.className = "user-name-row";
        const nome = document.createElement("span");
        nome.className = "user-name";
        nome.textContent = u.nome || u.usuario || "Sem nome";
        nomeRow.appendChild(nome);
        if (u.admin) {
            const badge = document.createElement("span");
            badge.className = "user-badge";
            badge.textContent = "Admin";
            nomeRow.appendChild(badge);
        }
        const handle = document.createElement("span");
        handle.className = "user-handle";
        handle.textContent = "@" + (u.usuario || "-");
        info.appendChild(nomeRow);
        info.appendChild(handle);
        cell.appendChild(avatar);
        cell.appendChild(info);
        tdUser.appendChild(cell);
        linha.appendChild(tdUser);

        const tdCpf = document.createElement("td");
        tdCpf.className = "cpf-cell";
        tdCpf.textContent = u.cpf || "—";
        linha.appendChild(tdCpf);

        const tdAdmin = document.createElement("td");
        tdAdmin.textContent = u.admin ? "sim" : "nao";
        linha.appendChild(tdAdmin);

        const tdAcao = document.createElement("td");
        const botao = document.createElement("button");
        botao.className = "btn-delete";
        botao.title = "Deletar usuario";
        botao.textContent = "×";
        botao.onclick = function () { deletarUsuario(u.id); };
        tdAcao.appendChild(botao);
        linha.appendChild(tdAcao);

        tbody.appendChild(linha);
    }
    tabela.appendChild(tbody);
    alvo.appendChild(tabela);
}

async function deletarUsuario(id) {
    if (!confirm("Deletar o usuario " + id + "?")) {
        return;
    }
    await enviarJson("/admin/deletar", { id: id });
    carregarAdmin();
}

// ----- Admin: transferencias -----
async function carregarTransferencias() {
    const alvo = document.getElementById("tabela_transferencias");
    if (!alvo) return;
    let transferencias;
    try {
        const resposta = await fetch(API + "/admin/transferencias");
        if (!resposta.ok) {
            alvo.textContent = await resposta.text();
            return;
        }
        transferencias = await resposta.json();
    } catch (e) {
        alvo.textContent = "Nao foi possivel carregar as transferencias.";
        return;
    }
    const contador = document.getElementById("transf-count");
    if (contador) contador.textContent = transferencias.length;

    alvo.innerHTML = "";
    if (transferencias.length === 0) {
        const vazio = document.createElement("div");
        vazio.className = "empty-state";
        vazio.innerHTML =
            '<div class="empty-icon">↔</div>' +
            '<div class="empty-title">Nenhuma transferencia</div>' +
            '<div class="empty-text">As transferencias feitas pelos clientes aparecem aqui.</div>';
        alvo.appendChild(vazio);
        return;
    }

    const tabela = document.createElement("table");

    const thead = document.createElement("thead");
    const cabecalho = document.createElement("tr");
    for (const titulo of ["ID", "Origem", "Destino", "Valor", "Data", ""]) {
        const th = document.createElement("th");
        th.textContent = titulo;
        cabecalho.appendChild(th);
    }
    thead.appendChild(cabecalho);
    tabela.appendChild(thead);

    const tbody = document.createElement("tbody");
    for (const t of transferencias) {
        const linha = document.createElement("tr");
        const valores = [
            String(t.id).padStart(2, "0"), t.origem, t.destino, t.valor, t.data || ""
        ];
        for (const v of valores) {
            const td = document.createElement("td");
            td.textContent = v;
            linha.appendChild(td);
        }
        const acao = document.createElement("td");
        const botao = document.createElement("button");
        botao.className = "btn-estorno";
        botao.textContent = "Estornar";
        botao.onclick = function () { estornarTransferencia(t.id); };
        acao.appendChild(botao);
        linha.appendChild(acao);
        tbody.appendChild(linha);
    }
    tabela.appendChild(tbody);
    alvo.appendChild(tabela);
}

async function estornarTransferencia(id) {
    if (!confirm("Estornar a transferencia " + id + "?")) {
        return;
    }
    await enviarJson("/admin/estornar", { id: id });
    carregarTransferencias();
}

document.addEventListener("DOMContentLoaded", function () {
    if (document.getElementById("ola")) {
        carregarUsuario();
    }
});
