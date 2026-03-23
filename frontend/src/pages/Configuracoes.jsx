import { useMemo, useState } from "react";
import { useTheme } from "../context/ThemeContext";
import { useAuth } from "../context/AuthContext";
import { apiJson } from "../services/api"; // ajuste o path se no seu projeto for outro
import { Link } from "react-router-dom";

export default function Configuracoes() {
  const { theme, toggleTheme } = useTheme();
  const { me } = useAuth();

  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmNewPassword, setConfirmNewPassword] = useState("");

  const [loading, setLoading] = useState(false);
  const [okMsg, setOkMsg] = useState("");
  const [errMsg, setErrMsg] = useState("");

  const canSubmit = useMemo(() => {
    if (!currentPassword || !newPassword || !confirmNewPassword) return false;
    if (newPassword.length < 8) return false;
    if (newPassword !== confirmNewPassword) return false;
    return true;
  }, [currentPassword, newPassword, confirmNewPassword]);

  async function onSubmit(e) {
    e.preventDefault();
    setOkMsg("");
    setErrMsg("");

    if (newPassword.length < 8) {
      setErrMsg("A nova senha precisa ter pelo menos 8 caracteres.");
      return;
    }
    if (newPassword !== confirmNewPassword) {
      setErrMsg("A confirmação não confere com a nova senha.");
      return;
    }

    try {
      setLoading(true);

      // ✅ endpoint novo do backend (você vai criar abaixo)
      await apiJson("/auth/password", "PUT", {
        currentPassword,
        newPassword,
      });

      setOkMsg("Senha atualizada com sucesso ✅");
      setCurrentPassword("");
      setNewPassword("");
      setConfirmNewPassword("");
    } catch (err) {
      // apiJson geralmente joga {status, payload} ou string
      const msg =
        err?.payload?.message ||
        err?.payload?.error ||
        err?.message ||
        "Não foi possível alterar a senha.";
      setErrMsg(msg);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="dashboard-shell">
      <main className="dashboard-main">
        <div style={{ marginBottom: 14 }}>
          <h1 className="dashboard-title" style={{ marginBottom: 6 }}>
            Configurações
          </h1>
          <p className="dashboard-subtitle">
            Segurança da conta + preferências básicas.
          </p>
        </div>

        <section className="settings-grid">
          {/* PERFIL */}
          <div className="panel">
            <h3 className="panel-title">Meu perfil</h3>
            <p className="panel-subtitle" style={{ marginTop: 6 }}>
              <strong>Nome:</strong> {me?.nome || "—"} <br />
              <strong>E-mail:</strong> {me?.email || "—"} <br />
              <strong>Role:</strong> {me?.role || "—"}
            </p>
          </div>

          {/* RESET SENHA */}
          <div className="panel">
            <h3 className="panel-title">Redefinir senha</h3>
            <p className="panel-subtitle" style={{ marginTop: 6 }}>
              Para sua segurança, confirme sua senha atual.
            </p>

            <form className="form-card" onSubmit={onSubmit} style={{ marginTop: 12 }}>
              <div className="form-field">
                <label>Senha atual</label>
                <input
                  type="password"
                  value={currentPassword}
                  onChange={(e) => setCurrentPassword(e.target.value)}
                  placeholder="Digite sua senha atual"
                  autoComplete="current-password"
                />
              </div>

              <div className="form-grid">
                <div className="form-field">
                  <label>Nova senha</label>
                  <input
                    type="password"
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                    placeholder="mín. 8 caracteres"
                    autoComplete="new-password"
                  />
                </div>

                <div className="form-field">
                  <label>Confirmar nova senha</label>
                  <input
                    type="password"
                    value={confirmNewPassword}
                    onChange={(e) => setConfirmNewPassword(e.target.value)}
                    placeholder="repita a nova senha"
                    autoComplete="new-password"
                  />
                </div>
              </div>

              {errMsg ? <div className="settings-alert error">{errMsg}</div> : null}
              {okMsg ? <div className="settings-alert ok">{okMsg}</div> : null}

              <div className="form-footer" style={{ justifyContent: "space-between" }}>
                {/* Ajusta esse path pro seu "esqueci senha" real */}
                <Link to="/recuperar-senha" className="settings-link">
                  Esqueci minha senha
                </Link>

                <button className="btn-primary btn-inline" disabled={!canSubmit || loading}>
                  {loading ? "Salvando..." : "Salvar nova senha"}
                </button>
              </div>
            </form>
          </div>

          {/* APARÊNCIA (opcional, mas acho bom manter) */}
          <div className="panel">
            <h3 className="panel-title">Aparência</h3>
            <p className="panel-subtitle" style={{ marginTop: 6 }}>
              Tema atual: <strong>{theme === "dark" ? "Escuro" : "Claro"}</strong>
            </p>

            <div style={{ marginTop: 10 }}>
              <button className="btn-secondary btn-inline" type="button" onClick={toggleTheme}>
                Alternar tema
              </button>
            </div>
          </div>
        </section>
      </main>
    </div>
  );
}
