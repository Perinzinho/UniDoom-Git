# Mapas — ponto de entrada para manutenção

Leia este índice e **comece pela seção Leitura rápida da ficha do mapa solicitado**. Abra outras seções apenas conforme a alteração. A ficha registra o estado atual; a cena e os assets continuam sendo a fonte técnica. Atualize-a quando mudar layout, dimensões, conexões ou dependências.

| Mapa | Cena | Ficha |
|---|---|---|
| Bloco de Direito | `Assets/_Project/Scenes/LawSchool/LawSchool.unity` | [LawSchool.md](LawSchool.md) |
| Bloco 4 — Tecnologia da Informação | `Assets/_Project/Scenes/CampusBlock4/CampusBlock4.unity` | [CampusBlock4.md](CampusBlock4.md) |
| Campus Exterior — Unimar | `Assets/_Project/Scenes/CampusExterior/CampusExterior.unity` | Dependência: seção Conexões de [LawSchool.md](LawSchool.md#conexões) |
| Biblioteca do Campus — Unimar | `Assets/_Project/Scenes/CampusLibrary/CampusLibrary.unity` | [CampusLibrary.md](CampusLibrary.md); contexto e portais em [README_CAMPUS_EXTERIOR.md](../../README_CAMPUS_EXTERIOR.md) |

- Instruções operacionais: [prompt mestre](../../.codex/PROMPT_MESTRE.md).
- Direção artística normativa: [Total Chaos visual direction](../../.codex/SKILL.md).
- Pesquisa de novos assets: [referências, fontes e autores](Referencias-e-Assets.md); leia apenas se precisar de modelos, materiais ou comparação visual.
- Regras de organização e preservação: [AGENTS.md](../../AGENTS.md).

Não carregue todas as cenas, este catálogo inteiro e os logs antigos por padrão. Localize caminhos com `rg --files`, consulte os campos afetados e amplie a leitura somente quando faltar evidência. Não marque um teste como executado apenas porque consta numa ficha anterior.

Contrato compartilhado: [chaves numeradas das celas](PrisonCellKeys.md) — Direito, Biblioteca, Bloco 4 e porão.

Menu compartilhado: [créditos, música e HUD da espingarda](../Credits.md).

## Encontros com zumbis — comportamento compartilhado

- `Enemy.cs`: percepção próxima em todas as direções até 3,5 m, ainda limitada por alcance e obstáculos; patrulhas olham ao redor nas pausas. Movimento desacelera enquanto o corpo vira. Ataques corpo a corpo acompanham o alvo na preparação e ataques à distância também exigem orientação para o alvo.
- `ClassRoom` (reforços da chave), `Library` (biblioteca antiga, ondas) e `Cylinder` (gatilho do átrio) dependem de `Global/Scripts/Enemy/EnemySpawnSafety.cs`. Reforços exigem distância horizontal mínima de 6 m e um volume fora do enquadramento ou oculto por colisores do cenário. Outros zumbis não contam como cobertura. Sem jogador/câmera, o spawn aguarda. Cada ponto aguarda até 12 s; se continuar inseguro, é dispensado, sem forçar aparição visível. Isso pode reduzir a quantidade da onda quando o jogador vigia os pontos.
- `BasementArenaEncounter` instancia seu grupo no `Awake`, visível e com IA desativada. A entrada na arena ativa os sobreviventes; o portão continua dependendo da morte dos inimigos registrados. Os corpos e os inimigos preexistentes continuam contabilizados conforme seu estado. O chefe usa um sistema separado, sem alteração nesta revisão.
- Spawns iniciais de `EnemySpawner` e guardas iniciais de `ClassRoom` mantêm o fluxo de carregamento. Cenas, posições, prefabs e GUIDs existentes não foram alterados nesta revisão.

Validação desta revisão (2026-11-06): revisão estática dos consumidores e do diff. Unity Editor não está disponível neste ambiente; compilação, carregamento das cenas/prefabs, Play Mode, build e comparação visual **não executados**. Na Unity, conferir aproximação por trás com/sem parede, perseguição em curvas, preparação dos ataques, ondas sob observação e após desviar a câmera, pausa durante a espera e liberação do portão do porão após eliminar os sobreviventes. Cobertura do spawn depende dos colisores; superfícies transparentes com colisão e frestas precisam de conferência visual.
