import { NavLink, useNavigate } from "react-router-dom";
import { useTheme } from "../context/ThemeContext";
import ThemeToggle from "./ThemeToggle";
import { useAuth } from "../context/AuthContext";
import { useEffect, useMemo, useRef, useState } from "react";
import { apiJson } from "../services/api";

function roleLinks(role) {
  if (role === "Admin") {
    return [
      { to: "/dashboard", label: "Dashboard" },
      { to: "/admin/alunos", label: "Alunos" },
      { to: "/admin/professores", label: "Professores" },
      { to: "/admin/agenda", label: "Agenda" },
    ];
  }

  if (role === "Professor") {
    return [
      { to: "/dashboard", label: "Dashboard" },
      { to: "/painel-professor", label: "Painel" },
      { to: "/professor/agenda", label: "Agenda" },
    ];
  }

  // Aluno
  return [
    { to: "/dashboard", label: "Dashboard" },
    { to: "/meu-painel", label: "Meu painel" },
  ];
}

export default function AppHeader() {
  const { theme } = useTheme();
  const { me, logout } = useAuth();
  const navigate = useNavigate();

  const [menuOpen, setMenuOpen] = useState(false);

  // 🔔 notif dropdown
  const [notifOpen, setNotifOpen] = useState(false);
  const [notifLoading, setNotifLoading] = useState(false);
  const [notifs, setNotifs] = useState([]);

  const menuRef = useRef(null);
  const notifRef = useRef(null);

  const links = useMemo(() => roleLinks(me?.role), [me?.role]);

  // fecha dropdowns clicando fora
  useEffect(() => {
    function onDocClick(e) {
      const menuEl = menuRef.current;
      const notifEl = notifRef.current;

      const clickedMenu = menuEl && menuEl.contains(e.target);
      const clickedNotif = notifEl && notifEl.contains(e.target);

      if (!clickedMenu) setMenuOpen(false);
      if (!clickedNotif) setNotifOpen(false);
    }

    document.addEventListener("mousedown", onDocClick);
    return () => document.removeEventListener("mousedown", onDocClick);
  }, []);

  // carregar notificações (mount)
  useEffect(() => {
    let alive = true;
    async function load() {
      try {
        const data = await apiJson("/notificacoes/me");
        if (!alive) return;
        setNotifs(Array.isArray(data) ? data : []);
      } catch {
        if (!alive) return;
        setNotifs([]);
      }
    }
    load();
    return () => (alive = false);
  }, []);

  // filtra só notificações DIRETAS (específicas do usuário)
  const directNotifs = useMemo(() => {
    const list = Array.isArray(notifs) ? notifs : [];
    return list.filter((n) => (n?.usuarioId ?? n?.UsuarioId) != null);
  }, [notifs]);

  const unreadCount = useMemo(() => {
    return directNotifs.filter((n) => (n?.lida ?? n?.Lida) === false).length;
  }, [directNotifs]);

  async function reloadNotifs() {
    try {
      setNotifLoading(true);
      const data = await apiJson("/notificacoes/me");
      setNotifs(Array.isArray(data) ? data : []);
    } catch {
      setNotifs([]);
    } finally {
      setNotifLoading(false);
    }
  }

  async function handleToggleNotif() {
    const next = !notifOpen;
    setNotifOpen(next);

    // ao abrir, recarrega pra ficar atualizado
    if (next) {
      await reloadNotifs();
    }
  }

  function handleLogout() {
    logout();
    navigate("/login");
  }

  return (
    <header className="app-header">
      <div className="app-header__left">
        <img
          className="app-header__logo"
          src={theme === "dark" ? "/educonnect-logo-dark.svg" : "/educonnect-logo.svg"}
          alt="Logo EduConnect"
          onClick={() => navigate("/dashboard")}
          style={{ cursor: "pointer" }}
        />
      </div>

      <div className="app-header__right">
        <ThemeToggle />

        <nav className="app-nav">
          {links.map((l) => (
            <NavLink
              key={l.to}
              to={l.to}
              end
              className={({ isActive }) => (isActive ? "active" : "")}
            >
              {l.label}
            </NavLink>
          ))}
        </nav>

        {/* Sino + Dropdown */}
        <div className="notif-wrap" ref={notifRef}>
          <button
            type="button"
            className="notif-btn"
            onClick={handleToggleNotif}
            title="Notificações"
            aria-label="Notificações"
          >
            🔔
            {unreadCount > 0 && <span className="notif-badge">{unreadCount}</span>}
          </button>

          {notifOpen && (
            <div className="notif-dropdown">
              <div className="notif-dropdown__header">
                <strong>Notificações</strong>
                <button
                  type="button"
                  className="btn-secondary btn-inline"
                  onClick={() => setNotifOpen(false)}
                >
                  Fechar
                </button>
              </div>

              {notifLoading ? (
                <p className="panel-subtitle" style={{ marginTop: 10 }}>
                  Carregando...
                </p>
              ) : directNotifs.length === 0 ? (
                <p className="panel-subtitle" style={{ marginTop: 10 }}>
                  Sem notificações diretas.
                </p>
              ) : (
                <ul className="notif-dropdown__list">
                  {directNotifs.slice(0, 10).map((n) => (
                    <li
                      key={n.id ?? n.Id}
                      className={`notif-top-item ${(n.lida ?? n.Lida) === false ? "unread" : ""}`}
                    >
                      <strong>{n.titulo ?? n.Titulo}</strong>
                      <div className="notif-item__msg">{n.mensagem ?? n.Mensagem}</div>
                    </li>
                  ))}
                </ul>
              )}

              <div className="notif-dropdown__footer">
                <button
                  type="button"
                  className="btn-secondary btn-inline"
                  onClick={() => {
                    setNotifOpen(false);
                    if (me?.role === "Aluno") navigate("/meu-painel");
                    else if (me?.role === "Professor") navigate("/professor/agenda");
                    else navigate("/admin/agenda");
                  }}
                >
                  Ver todas
                </button>

                <button
                  type="button"
                  className="btn-secondary btn-inline"
                  onClick={reloadNotifs}
                >
                  Atualizar
                </button>
              </div>
            </div>
          )}
        </div>

        {/* Usuário / Dropdown */}
        <div className="user-menu" ref={menuRef}>
          <button
            type="button"
            className="user-chip"
            onClick={() => setMenuOpen((v) => !v)}
            title="Menu do usuário"
          >
            <span className="user-avatar">{(me?.nome?.[0] || "U").toUpperCase()}</span>
            <span className="user-name">{me?.nome || "Usuário"}</span>
            <span className="user-caret">▾</span>
          </button>

          {menuOpen && (
            <div className="user-dropdown">
              <div className="user-dropdown__meta">
                <div className="user-dropdown__title">{me?.nome || "Usuário"}</div>
                <div className="user-dropdown__sub">{me?.email || ""}</div>
                <div className="user-dropdown__role">{me?.role || ""}</div>
              </div>

              <button
                type="button"
                onClick={() => {
                  setMenuOpen(false);
                  navigate("/config");
                }}
              >
                Configurações
              </button>

              <button type="button" onClick={handleLogout}>
                Sair
              </button>
            </div>
          )}
        </div>
      </div>
    </header>
  );
}
