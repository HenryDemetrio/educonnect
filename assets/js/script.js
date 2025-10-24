document.getElementById("login-form").addEventListener("submit", function (event) {
  event.preventDefault();

  const email = document.getElementById("email").value.trim();
  const password = document.getElementById("password").value.trim();
  const errorDiv = document.getElementById("error-message");

  // Expressão: mínimo 8 caracteres, pelo menos 1 letra e 1 número
  const senhaValida = /^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8,}$/;

  if (!email || !password) {
    errorDiv.textContent = "Preencha todos os campos.";
    return;
  }

  if (!senhaValida.test(password)) {
    errorDiv.textContent =
      "A senha deve ter no mínimo 8 caracteres, com pelo menos 1 letra e 1 número.";
    return;
  }

  // Se tudo certo, simula login
  errorDiv.textContent = "";
  window.location.href = "dashboard.html";
});
