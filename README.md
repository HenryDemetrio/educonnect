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


🧱 Estrutura de Interface

✅ Página de Login responsiva (index.html)

✅ Dashboard com menu de navegação (dashboard.html)

 Tema claro/escuro com botão de alternância

📋 Listagens e Formulários

 Página de Cadastro de Aluno com formulário validado

 Página de Listagem de Alunos com tabela e filtro

 Página de Cadastro de Professor com formulário validado

 Página de Listagem de Professores com tabela e filtro

✨ Interatividade

✅ Validação de formulário de login com JavaScript

 Validação de cadastro de aluno e professor com JavaScript

 Gráfico de desempenho (ex: notas ou frequência com Chart.js)

 Sistema de notificações (mensagens simples no dashboard)

 Calendário escolar interativo

🎨 Identidade Visual

 ✅ Paleta de cores baseada no vermelho da TIVIT

 ✅ Logos da TIVIT, FIAP e Alura adicionados

 Ícones ou imagens adicionais para reforçar o visual

🌟 Opcionais (extras que agregam valor)

 Tema escuro com localStorage

 Feedback visual após submissão de formulário

 Layout com Flexbox ou CSS Grid

 Responsividade completa no mobile