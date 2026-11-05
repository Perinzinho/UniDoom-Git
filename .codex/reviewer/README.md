# Unity Map Reviewer

Pacote de skill independente para revisar mapas Unity conforme regras de criação que você já possui.

## Instalação
Copie a pasta `unity-map-reviewer` para o diretório de skills da sua ferramenta de IA (o caminho depende do agente/IDE). Aponte o agente para `SKILL.md` se sua ferramenta não possuir carregamento automático de skills.

## Uso
Abra `PROMPT-DE-EXECUCAO.md`, substitua os campos entre colchetes pelos caminhos reais do seu projeto, skill e prompt originais e envie o texto ao agente com acesso aos arquivos do projeto e, idealmente, ao Unity Editor.

## Observações
A skill é agnóstica ao agente. Sem acesso ao Unity Editor, ela pode auditar arquivos e fazer apenas validações estáticas; não equivale a testes de cena no Editor. Evite commits automáticos e faça backup/commit prévio antes de alterações em massa.
