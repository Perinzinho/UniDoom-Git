# Mapas — ponto de entrada para manutenção

Leia este índice e **comece pela seção Leitura rápida da ficha do mapa solicitado**. Abra outras seções apenas conforme a alteração. A ficha registra o estado atual; a cena e os assets continuam sendo a fonte técnica. Atualize-a quando mudar layout, dimensões, conexões ou dependências.

| Mapa | Cena | Ficha |
|---|---|---|
| Bloco de Direito | `Assets/_Project/Scenes/LawSchool/LawSchool.unity` | [LawSchool.md](LawSchool.md) |
| Campus Exterior — Unimar | `Assets/_Project/Scenes/CampusExterior/CampusExterior.unity` | Dependência: seção Conexões de [LawSchool.md](LawSchool.md#conexões) |
| Porão e prisão | `Assets/_Project/Scenes/Basement/Basement.unity` | [Basement.md](Basement.md) |

- Instruções operacionais: [prompt mestre](../../.codex/PROMPT_MESTRE.md).
- Direção artística normativa: [Total Chaos visual direction](../../.codex/SKILL.md).
- Pesquisa de novos assets: [referências, fontes e autores](Referencias-e-Assets.md); leia apenas se precisar de modelos, materiais ou comparação visual.
- Regras de organização e preservação: [AGENTS.md](../../AGENTS.md).

Não carregue todas as cenas, este catálogo inteiro e os logs antigos por padrão. Localize caminhos com `rg --files`, consulte os campos afetados e amplie a leitura somente quando faltar evidência. Não marque um teste como executado apenas porque consta numa ficha anterior.

## Transições de ambiente — reload e arte de chegada

- Toda troca de cena passa por `DoorReloadScreen`: a tela cobre a transição e bloqueia movimento/câmera, posiciona o jogador no `SceneArrivalPoint`, aquece primeiro a coleção opcional e depois todos os shaders carregados, aguarda um frame já aquecido e somente então libera o jogador.
- Cada destino conhecido usa `Resources/Textures/UI/Transitions/<Cena>.png`. As artes são 16:9, baseadas na visão real de chegada e mantêm o padrão cinematográfico iniciado em `Cylinder`. Destinos sem arte cadastrada usam `Global/Resources/Textures/UI/Transitions/AbandonedDoor.png`.

## Encontros com zumbis — comportamento compartilhado

- `Enemy.cs`: percepção próxima em todas as direções até 3,5 m, ainda limitada por alcance e obstáculos; patrulhas olham ao redor nas pausas. Movimento desacelera enquanto o corpo vira. Ataques corpo a corpo acompanham o alvo na preparação e ataques à distância também exigem orientação para o alvo.
- `ClassRoom` (reforços da chave), `Library` (biblioteca antiga, ondas) e `Cylinder` (gatilho do átrio) dependem de `Global/Scripts/Enemy/EnemySpawnSafety.cs`. Reforços exigem distância horizontal mínima de 6 m e um volume fora do enquadramento ou oculto por colisores do cenário. Outros zumbis não contam como cobertura. Sem jogador/câmera, o spawn aguarda. Cada ponto aguarda até 12 s; se continuar inseguro, é dispensado, sem forçar aparição visível. Isso pode reduzir a quantidade da onda quando o jogador vigia os pontos.
- `BasementArenaEncounter` instancia seu grupo no `Awake`, visível e com IA desativada. A entrada na arena ativa os sobreviventes; o portão continua dependendo da morte dos inimigos registrados. Os corpos e os inimigos preexistentes continuam contabilizados conforme seu estado. O chefe usa um sistema separado, sem alteração nesta revisão.
- Spawns iniciais de `EnemySpawner` e guardas iniciais de `ClassRoom` mantêm o fluxo de carregamento. Cenas, posições, prefabs e GUIDs existentes não foram alterados nesta revisão.

## Portas comuns — abertura por contato

- `OpenDoorWithKey` sem `requiresKey01`, `requiresKey02` ou `requiresKey03` não abre mais ao entrar no trigger de proximidade. O jogador precisa avançar contra o collider físico da folha; `PlayerMovement.OnControllerColliderHit` então solicita a abertura.
- Após fechar completamente e o jogador deixar o trigger, a porta comum volta a exigir novo contato. Portas com chave, portas de ativação manual, direção da dobradiça, fechamento automático e transições de setor preservam seus fluxos.
- `TCSimpleDoor` com `keyId` vazio segue a mesma regra quando `autoOpen` está habilitado; o antigo raio baseado na câmera não abre mais a porta.
- Inventário estático desta revisão (2026-11-09): 212 componentes `OpenDoorWithKey`, sendo 208 sem chave, 4 com chave e 1 com ativação manual. Compilação em Unity 6000.5.7f1 concluída sem erros; teste de contato em Play Mode não executado.

Validação desta revisão (2026-11-06): revisão estática dos consumidores e do diff. Unity Editor não está disponível neste ambiente; compilação, carregamento das cenas/prefabs, Play Mode, build e comparação visual **não executados**. Na Unity, conferir aproximação por trás com/sem parede, perseguição em curvas, preparação dos ataques, ondas sob observação e após desviar a câmera, pausa durante a espera e liberação do portão do porão após eliminar os sobreviventes. Cobertura do spawn depende dos colisores; superfícies transparentes com colisão e frestas precisam de conferência visual.
