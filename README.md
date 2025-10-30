Funcionalidades
Login (index.html): Formulário responsivo com redirecionamento simulado para o dashboard.

Dashboard: Menu de navegação, gráfico de desempenho, calendário interativo, notificações e alternância de tema claro/escuro.

Listagens (students.html / teachers.html): Tabelas com filtro de busca, botões de ação e link para cadastro.

Cadastros (add_student.html / add_teacher.html): Formulários com validações em JS e redirecionamento simulado.

CSS: Estilo unificado e responsivo para todas as páginas, com suporte a temas.

JS: Scripts para tema, validações, filtros, gráfico, calendário e notificações.


ESQUEMA DE PASTAS:

projeto/
├── index.html              -> Página de **Login** (página inicial do sistema)
├── dashboard.html          -> Página **Dashboard** pós-login (contém menu, gráficos, etc.)
├── students.html           -> Página de **Lista de Alunos** (tabela com filtro de alunos)
├── add_student.html        -> Página de **Cadastro de Aluno** (formulário de aluno)
├── teachers.html           -> Página de **Lista de Professores** (tabela com filtro de professores)
├── add_teacher.html        -> Página de **Cadastro de Professor** (formulário de professor)
├── assets/                 -> Pasta de arquivos estáticos (CSS, JS, imagens, etc.)
│   ├── css/
│   │   └── style.css       -> Arquivo de estilos **CSS** global (vale para todas as páginas)
│   ├── js/
│   │   └── script.js       -> Arquivo **JavaScript** principal (interatividade: tema, validações, etc.)
│   └── images/             -> Pasta para **imagens** (logos, ícones, etc. caso necessário)

 Tema escuro com localStorage

 Feedback visual após submissão de formulário

 Layout com Flexbox ou CSS Grid

 Responsividade completa no mobile
