---
name: unity-map-reviewer
description: Audita, padroniza e valida mapas/cenas Unity existentes conforme a skill e o prompt oficiais do projeto, com edições mínimas, processamento incremental e economia de tokens.
---

# Unity Map Reviewer

## Objetivo
Revisar TODOS os mapas/cenas Unity elegíveis que a IA criou ou modificou, usando a skill e o prompt de criação já existentes como **fonte normativa**, sem inventar padrões. Corrigir somente desvios comprovados, preservar funcionamento e registrar progresso para execução incremental.

## Entradas obrigatórias
- **SKILL_ORIGINAL**: caminho da skill usada para criar/padronizar os mapas.
- **PROMPT_ORIGINAL**: caminho do prompt complementar, quando existir.
- **PROJECT_ROOT**: raiz do projeto Unity.

Entradas opcionais: `MAP_GLOBS`, `MAP_MANIFEST`, `EXCLUDE_GLOBS`, `BATCH_SIZE` (padrão: 5), `MODE` (`audit`, `fix`, `validate`; padrão `fix`), `REPORT_DIR` (padrão `.ai/map-review`), `APPROVAL_REQUIRED_FOR_DESTRUCTIVE` (padrão `true`).

Se faltar a fonte normativa, **não alterar cenas**: gerar inventário e lista objetiva das entradas pendentes. Se as regras forem ambíguas ou conflitantes, preservar a cena e relatar o bloqueio. Hierarquia: instrução atual explícita > prompt original específico > skill original > padrões comprovados no próprio projeto; nunca inferir requisitos apenas por gosto estético.

## Escopo e descoberta
1. Ler somente os cabeçalhos, índices ou seções pertinentes da skill/prompt originais e produzir uma checklist curta de requisitos verificáveis. Expandir trechos apenas quando necessários.
2. Inventariar arquivos por caminhos (não ler conteúdo completo inicialmente): `Assets/**/*.unity`, cenas de mapas por manifesto, pastas e convenções locais. Usar `Packages/manifest.json` apenas quando relevante para dependências. Excluir `Library/`, `Temp/`, `Obj/`, `Logs/`, `Build*/`, `.git/`, `UserSettings/`.
3. Distinguir mapas de cenas de menu, testes ou exemplos. Se não for possível saber quais mapas foram criados pela IA, revisar **todas as cenas elegíveis** e sinalizar que a autoria não foi confirmada; nunca afirmar autoria sem evidência.
4. Respeitar `MAP_GLOBS`/`MAP_MANIFEST`; registrar todos os caminhos incluídos/excluídos e o motivo.
5. Detectar versão do Unity em `ProjectSettings/ProjectVersion.txt` somente se isso impactar a checagem.

## Estratégia econômica de auditoria
- **Etapa A — Índice compacto:** caminhos, tamanho, hash, última modificação, dependências/referências relevantes e estado de cada mapa. Não despejar YAML completo nem logs extensos no contexto.
- **Etapa B — Baseline:** selecionar uma cena já conforme (se houver) ou extrair apenas requisitos explícitos; não transformar um mapa arbitrário em regra.
- **Etapa C — Diff por regra:** abrir somente cenas com sinais de divergência; buscar propriedades/objetos específicos por script ou pesquisa seletiva, em vez de reler tudo.
- **Etapa D — Agrupar por causa:** aplicar soluções equivalentes em lotes, sem repetir justificativas para cada mapa.
- **Etapa E — Validação incremental:** validar somente cenas alteradas e dependências afetadas; realizar checagem final do inventário inteiro.
- Registrar hash + versão das regras. Se arquivo e regras não mudaram, reutilizar resultado da checagem anterior; invalidar o cache se mudar dependência relevante.
- Usar saída compacta: `arquivo | regra | problema | ação | resultado`, contagens e bloqueios. Detalhes longos vão para relatório em disco, não para o chat.

## Regras de edição seguras
1. Preferir **Unity Editor API** (scripts Editor, `EditorSceneManager`, `SerializedObject`, `PrefabUtility`, Undo quando aplicável) para cenas, prefabs e objetos serializados. Para projetos com ferramentas MCP/automação Unity, usar recursos equivalentes que preservem referências.
2. Alterar YAML/texto diretamente **somente** quando houver suporte comprovado, serialização em texto e mudança pequena/segura; não renumerar `fileID`, GUID, referências, nem reformatar cenas inteiras.
3. Não excluir objetos, Tilemaps, layers, sorting layers, colliders, lighting, scripts, eventos, componentes ou prefabs sem exigência explícita. Nunca substituir uma cena inteira por outra para “padronizar”.
4. Preservar arquivos `.meta` e GUIDs. Manter alterações fora do escopo intactas. Antes de editar, inspecionar `git status`; se houver mudanças preexistentes no mesmo arquivo, não sobrescrever sem conciliação segura.
5. Não executar ações destrutivas ou migrações irreversíveis sem confirmação, quando `APPROVAL_REQUIRED_FOR_DESTRUCTIVE=true`.
6. Se Unity Editor ou ferramentas de validação estiverem indisponíveis, realizar apenas checagens estáticas verificáveis e registrar claramente o que **não** foi validado. Não inventar sucesso de teste.
7. Em caso de falha, interromper o lote afetado e preservar cópia/diff reversível; não avançar silenciosamente com cenas inválidas.

## Checklist de verificação
Construir os itens abaixo **apenas se estiverem exigidos pela fonte normativa ou claramente necessários à integridade técnica**:
- Estrutura de hierarquia, nomes, organização e objetos raiz.
- Prefabs, componentes, scripts, tags, layers e sorting layers.
- Tilemaps, Grid, escala, dimensões, pivôs, colisões e navegação.
- Materiais, sprites, iluminação, câmeras e consistência visual definida.
- Referências de assets, GUID/fileID e links quebrados.
- Performance conforme regras (ex.: batching, objetos duplicados, componentes custosos).
- Configurações específicas de mapas e critérios de jogabilidade especificados.

Classificar cada item por mapa: `PASS`, `FAIL`, `BLOCKED`, `NOT_APPLICABLE` ou `NOT_VERIFIED`. Não marcar `PASS` sem evidência.

## Procedimento de execução
1. **Preparar:** ler fontes normativas, montar checklist, registrar versão/hash das regras, inspecionar estado do repositório.
2. **Descobrir:** listar todas as cenas elegíveis, deduplicar e gerar manifesto com estados `pending`, `audited`, `fixed`, `validated`, `blocked`.
3. **Auditar:** percorrer por lotes de `BATCH_SIZE`; priorizar integridade/referências, depois padrões estruturais, depois visuais e otimizações.
4. **Planejar diff:** para cada falha, registrar regra original, evidência local, mudança mínima, risco e critério de validação.
5. **Corrigir:** aplicar somente mudanças seguras e justificadas. Em caso de conflito ou risco destrutivo, marcar `blocked` e continuar com cenas independentes.
6. **Validar:** no mínimo, conferir integridade de referências e comparação antes/depois; se houver Editor disponível, abrir cenas alteradas, verificar erros de Console relacionados, executar validações ou testes pertinentes, salvar e reabrir quando viável.
7. **Encerrar:** atualizar manifesto e relatório resumido; confirmar número total de cenas encontradas, auditadas, corrigidas, validadas, bloqueadas e não verificadas. Nunca dizer “todos corrigidos” se houver pendências.

## Persistência / arquivos produzidos
Manter em `REPORT_DIR`:
- `map-manifest.json`: caminho, hash, status, regras aplicadas, último resultado, dependências relevantes.
- `rules-checklist.md`: requisitos resumidos com indicação da origem (arquivo/seção).
- `review-report.md`: resumo, diferenças corrigidas, pendências, riscos e testes efetivamente executados.

Escrever arquivos de forma incremental/atômica quando possível. Se algum deles já existir, atualizar preservando histórico relevante. Usar caminhos relativos; evitar colar conteúdo integral de cenas ou fontes normativas no relatório.

## Política de tokens
- Não duplicar o prompt original nem a skill original dentro da resposta ou do relatório: referenciá-los por caminho e seção.
- Não imprimir arquivo inteiro ou logs extensos. Buscar trechos mínimos que sustentem cada decisão.
- Usar scripts determinísticos para descoberta, hashes, enumeração e checagens repetitivas.
- Processar vários mapas com a mesma falha por regra compartilhada, sem repetir análises narrativas.
- Emitir progresso em resumo por lote, sem análise longa por arquivo.
- Reexecutar somente mapas alterados, bloqueados ou invalidados por mudanças nas regras/dependências.
- Não economizar tokens à custa de pular verificação crítica ou afirmar validação sem teste.

## Formato obrigatório de resposta final
`Escopo: N cenas identificadas; N elegíveis; N excluídas.`
`Resultado: N auditadas; N corrigidas; N validadas; N bloqueadas; N não verificadas.`
`Padrões aplicados: [resumo curto e referências às seções originais].`
`Pendências: [só as que exigem ação].`
`Artefatos: [caminhos do manifesto, checklist e relatório].`
`Testes: [o que foi executado; o que não pôde ser executado].`

## Critério de conclusão
Concluir apenas quando **todos os mapas elegíveis estiverem inventariados e auditados**, com cada falha corrigida e validada ou explicitamente bloqueada/pendente. Nunca considerar ausência de erro estático como validação completa dentro do Unity Editor.
