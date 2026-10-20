# Checklist normativa

Fontes: `.codex/reviewer/SKILL.md`, `.codex/reviewer/PROMPT-DE-EXECUCAO.md`, `.codex/SKILL.md` (direção visual), `.codex/PROMPT_MESTRE.md`, `AGENTS.md` e exclusão explícita da sala do diretor. Os campos do prompt foram resolvidos no manifesto, sem alterar os templates do usuário.

| Regra | Exigência verificável | Origem |
|---|---|---|
| R01 | GUID/fileID e dependências resolvidos; investigar referências faltantes sem substituição arbitrária | Reviewer / Checklist e Edição segura; mestre / regras 1 e 7 |
| R02 | Preservar cenas, prefabs, importadores, overrides, scripts, tags/layers e trabalho anterior | Mestre / regras 2 e 7; AGENTS |
| R03 | Reutilizar imagens existentes; recursos compartilhados em Global; mover com AssetDatabase.MoveAsset | AGENTS / Organização |
| R04 | Confirmar Unity/pipeline; abrir cenas, inspecionar scripts e shaders no Editor | Direção visual / Implementação; mestre / regras 1 e 8 |
| R05 | Materiais distinguíveis, PBR quando compatível, escala de textura coerente | Direção visual / Texturas e materiais |
| R06 | Arquitetura funcional, escala plausível, desgaste causal, composição e narrativa próprias | Direção visual / Princípios, Geometria, Desgaste e Composição |
| R07 | Fontes luminosas plausíveis, sombras navegáveis e efeitos moderados | Direção visual / Iluminação |
| R08 | Colisão, navegação, chegada por portas, encontros e interação preservados | Reviewer / Checklist; direção visual / Composição |
| R09 | Otimizar só com evidência; medir CPU/GPU em ms, memória, GC, SetPass e draw calls no alvo | Mestre / regras 1, 3, 6 e 10 |
| R10 | Comparar antes/depois e registrar testes reais; nunca equiparar auditoria estática à aprovação visual | Reviewer / Validar e Conclusão; mestre / regras 8 e 10 |

`PASS` exige evidência. `BLOCKED` indica impedimento concreto à conclusão/correção. `NOT_VERIFIED` indica verificação não executada. `NOT_APPLICABLE` não é aprovação visual.

O checklist visual original tem 12 critérios (escala, materiais, desgaste, luz, sombras/rota, profundidade, abandono, props, legibilidade, identidade, pós e desempenho). A pontuação permanece `null`, sem aprovação 18/24: inspeção serializada não permite atribuir notas de aparência ou desempenho. As seis cenas possuem estados individuais no manifesto.
