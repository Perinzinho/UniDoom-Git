# Prompt de execução — Unity Map Reviewer

Ative a skill `unity-map-reviewer` e faça uma auditoria completa e incremental de todos os mapas/cenas Unity elegíveis deste projeto.

**Fonte de verdade (preencher):**
- `PROJECT_ROOT`: `[CAMINHO/DA/RAIZ/DO/PROJETO]`
- `SKILL_ORIGINAL`: `[CAMINHO/DA/SKILL/DE/CRIACAO]`
- `PROMPT_ORIGINAL`: `[CAMINHO/DO/PROMPT/DE/CRIACAO]`
- `MAP_GLOBS` ou `MAP_MANIFEST`: `[OPCIONAL; caso vazio, descobrir as cenas relevantes]`
- `BATCH_SIZE`: `5`
- `MODE`: `fix`
- `REPORT_DIR`: `.ai/map-review`

Use minhas regras originais como padrão obrigatório. Primeiro descubra e inventarie os mapas; depois compare-os com as regras e corrija **somente desvios comprovados**, preservando referências, GUIDs, prefabs, estrutura funcional e modificações preexistentes. Não redesenhe ou recrie mapas inteiros.

**Otimização de tokens:** leia somente trechos pertinentes das regras e cenas, use diffs e scripts de inventário, agrupe problemas repetidos por regra, trabalhe em lotes e persista estado para retomar sem reanalisar mapas que não mudaram. Evite reproduzir YAML, arquivos completos ou logs enormes no chat.

Faça checagens estáticas e, se houver acesso real ao Unity Editor, valide as cenas alteradas nele. Nunca declare testes realizados sem evidência. Se regras estiverem ausentes ou contraditórias, ou se a correção for destrutiva, registre bloqueio e não invente uma solução.

Ao terminar, apresente contagem de mapas identificados/auditados/corrigidos/validados/bloqueados, resumo das mudanças, pendências e caminhos de `map-manifest.json`, `rules-checklist.md` e `review-report.md`. Não considere o trabalho concluído enquanto alguma cena elegível não tiver ao menos sido auditada.
