# Mapas — ponto de entrada para manutenção

Leia este índice e **comece pela seção Leitura rápida da ficha do mapa solicitado**. Abra outras seções apenas conforme a alteração. A ficha registra o estado atual; a cena e os assets continuam sendo a fonte técnica. Atualize-a quando mudar layout, dimensões, conexões ou dependências.

| Mapa | Cena | Ficha |
|---|---|---|
| Bloco de Direito | `Assets/_Project/Scenes/LawSchool/LawSchool.unity` | [LawSchool.md](LawSchool.md) |
| Bloco 4 — Tecnologia da Informação | `Assets/_Project/Scenes/CampusBlock4/CampusBlock4.unity` | [CampusBlock4.md](CampusBlock4.md) |
| Campus Exterior — Unimar | `Assets/_Project/Scenes/CampusExterior/CampusExterior.unity` | Dependência: seção Conexões de [LawSchool.md](LawSchool.md#conexões) |
| Porão e prisão | `Assets/_Project/Scenes/Basement/Basement.unity` | [Basement.md](Basement.md) |
| Biblioteca do Campus — Unimar | `Assets/_Project/Scenes/CampusLibrary/CampusLibrary.unity` | [CampusLibrary.md](CampusLibrary.md); contexto e portais em [README_CAMPUS_EXTERIOR.md](../../README_CAMPUS_EXTERIOR.md) |

Padrão de placas em todas as cenas: [Signage.md](Signage.md).

- Instruções operacionais: [prompt mestre](../../.codex/PROMPT_MESTRE.md).
- Direção artística normativa: [Total Chaos visual direction](../../.codex/SKILL.md).
- Pesquisa de novos assets: [referências, fontes e autores](Referencias-e-Assets.md); leia apenas se precisar de modelos, materiais ou comparação visual.
- Regras de organização e preservação: [AGENTS.md](../../AGENTS.md).

Não carregue todas as cenas, este catálogo inteiro e os logs antigos por padrão. Localize caminhos com `rg --files`, consulte os campos afetados e amplie a leitura somente quando faltar evidência. Não marque um teste como executado apenas porque consta numa ficha anterior.

## Transições de ambiente — reload e arte de chegada

- **Cobertura até a cena pronta (11/11/2026):** a arte de destino permanece sobre portais, reloads locais, retorno ao checkpoint e entrada pelo menu de mapas enquanto a cena termina de carregar, a coleção opcional de variantes aquece, a câmera do jogo renderiza três quadros do destino e, no Editor, a fila de compilação assíncrona de shaders esvazia. O menu mantém o fade preto até a arte ficar opaca. O aquecimento global de shaders continua desativado devido ao histórico de falha D3D12. Compilação C# isolada passou com avisos anteriores; a Unity em lote saiu com código 1 antes de compilar. Sem Play Mode, build ou confirmação de variantes de materiais ainda não vistos pela câmera.

- Toda troca de cena passa por `DoorReloadScreen`: a tela cobre a transição e bloqueia movimento/câmera, posiciona o jogador no `SceneArrivalPoint`, aquece a coleção opcional quando atribuída e deixa o cenário renderizar sob a cobertura antes de liberar o jogador. O aquecimento global de todos os shaders foi removido em 10/11/2026 devido às falhas observadas nos passes internos de URP/Direct3D 12.
- Cada destino conhecido usa `Resources/Textures/UI/Transitions/<Cena>.png`. As artes são 16:9, baseadas na visão real de chegada e mantêm o padrão cinematográfico iniciado em `Cylinder`. Destinos sem arte cadastrada usam `Global/Resources/Textures/UI/Transitions/AbandonedDoor.png`.
Contrato compartilhado: [chaves numeradas das celas](PrisonCellKeys.md) — Direito, Biblioteca, Bloco 4 e porão.

## Recuperação de texturas e sprites — 10/11/2026

O commit `497be021` preservou as imagens, mas regenerou GUIDs de **455 importadores**: 405 PNGs, um JPG, um JPEG, um HDR, 20 FBX, um OBJ, 16 WAVs e dez OGGs. Materiais, prefabs e cenas continuavam apontando para os identificadores anteriores, deixando texturas brancas e sprites ausentes em vários mapas. Os importadores de sprites também perderam recortes e IDs de subassets. Nenhum dos 455 `.meta` sofreu outra alteração entre esse commit e `d34b0765`.

Restaurados somente esses `.meta` a partir de `497be021^`, incluindo GUIDs, recortes, pivôs, escala de importação, canais alpha, normal maps e IDs dos sprites/modelos. As referências já existentes voltam a resolver sem reescrever cenas, materiais ou prefabs. As mudanças recentes de layout, manutenção, gerador, controles e demais sistemas foram preservadas. Os 804 arquivos de imagem/atlas rastreados estavam presentes e completos, sem ponteiros LFS pendentes; seu conteúdo foi conferido contra o Git/LFS e não foi editado.

Conferências sem abrir Unity: os 455 importadores coincidem com a versão anterior ao defeito; 401 GUIDs antigos voltam a atender consumidores existentes; 802 referências de textura/sprite em 365 assets foram conferidas com os IDs restaurados. GUIDs de pacotes foram distinguidos dos assets do projeto. Não foram alteradas referências históricas remanescentes de debug de probes, componentes do perfil padrão e sprite padrão de TMP que não pertencem à regeneração de `497be021`.

O log anterior da entrada no porão (`Logs/Editor-prev.log`) contém falhas de keyword space e buffers de shaders internos sob `Shader.WarmupAllShaders`, seguidas de crash nativo durante travessia de objetos pelo Mono. `DoorReloadScreen` deixou de chamar o aquecimento global; mantém a coleção opcional, a cobertura opaca e o bloqueio de controles até o primeiro quadro renderizado. Essa correção evita o caminho de aquecimento problemático identificado, mas a eliminação do crash precisa ser confirmada pelo usuário no jogo; não foi reproduzido nem certificado um diagnóstico exclusivo da falha nativa.

Compilação C# isolada passou sem erros, com avisos preexistentes. Sem iniciar/focar Unity, reimportação forçada, Play Mode, jogo, build, screenshots ou vídeos. Nenhum cache foi apagado e nenhum backup/quarentena permanente foi criado; temporários de investigação removidos ao concluir.

Menu compartilhado: [créditos, música e HUD da espingarda](../Credits.md).

## Munição ilimitada do jogador — 13/11/2026

- Pistola e espingarda mantêm o carregador, o consumo por disparo e suas animações/tempos de recarga, mas a reserva do jogador é ilimitada e não diminui ao recarregar.
- O HUD mostra `∞` no lugar da quantidade da reserva. Valores antigos de reserva permanecem serializados nos prefabs somente para preservar compatibilidade; não limitam mais o jogador.
- Como a razão de munição disponível permanece cheia, `EnemyLootDrop` não gera novas caixas por escassez. Caixas já posicionadas e seus assets foram preservados.
- Compilação e conferência estática executadas; disparo, recarga de pistola/espingarda, HUD em jogo e Play Mode ainda precisam de validação.

## Escala das caixas pequenas de munição — 13/11/2026

- Os pickups de 10 munições que reutilizam `Global/Prefabs/Weapons/FullAmmoBox.prefab` foram padronizados para aproximadamente **0,36 × 0,33 m**, equivalentes à escala visual `4 × 3 × 1` do sprite. O tamanho coincide com o padrão já usado no porão e fica deliberadamente menor que os medkits e a caixa UAC de 50 munições.
- Overrides antigos de `8 × 6` e `12 × 8` foram corrigidos em EntranceGarden, EntranceHallway, Cylinder, ClassRoom, DirectorRoom e Library. Corredor de entrada e diretoria mantêm compensações locais para as escalas herdadas de seus pais; o tamanho final é o mesmo.
- `Ammo.prefab`, a variante de 20 munições de LawSchool e `UACAmmoCrate50.prefab` já possuíam dimensões próprias coerentes e foram preservados. GUIDs, fileIDs, posições, valores de coleta e importadores não mudaram.
- Conferência estática de todas as 16 instâncias concluída, incluindo a escala efetiva herdada dos pais. CampusLibrary foi aberta na Unity; Play Mode e comparação visual cena a cena ainda precisam de validação. Ao reimportar `Cylinder.prefab`, a Unity também voltou a sinalizar referências `MonoBehaviour` ausentes preexistentes em objetos de iluminação (`Cold fluorescent spill` e `Light`); o diff dessa cena permanece limitado aos quatro valores de escala das duas caixas.

## Encontros com zumbis — comportamento compartilhado

- `Enemy.cs`: percepção próxima em todas as direções até 3,5 m, ainda limitada por alcance e obstáculos; patrulhas olham ao redor nas pausas. Movimento desacelera enquanto o corpo vira. Ataques corpo a corpo acompanham o alvo na preparação e ataques à distância também exigem orientação para o alvo.
- `ClassRoom` (reforços da chave), `Library` (biblioteca antiga, ondas) e `Cylinder` (gatilho do átrio) dependem de `Global/Scripts/Enemy/EnemySpawnSafety.cs`. Reforços exigem distância horizontal mínima de 6 m e um volume fora do enquadramento ou oculto por colisores do cenário. Outros zumbis não contam como cobertura. Sem jogador/câmera, o spawn aguarda. Cada ponto aguarda até 12 s; se continuar inseguro, é dispensado, sem forçar aparição visível. Isso pode reduzir a quantidade da onda quando o jogador vigia os pontos.
- `BasementArenaEncounter` instancia seu grupo no `Awake`, visível e com IA desativada. A entrada na arena ativa os sobreviventes; o portão continua dependendo da morte dos inimigos registrados. Os corpos e os inimigos preexistentes continuam contabilizados conforme seu estado. O chefe usa um sistema separado, sem alteração nesta revisão.
- Spawns iniciais de `EnemySpawner` e guardas iniciais de `ClassRoom` mantêm o fluxo de carregamento. Cenas, posições, prefabs e GUIDs existentes não foram alterados nesta revisão.

## Portas comuns — abertura por contato

- Portas que começam bloqueadas por uma chave exibem, ao contato, o aviso padronizado `Porta trancada! Procure pela chave.` por meio de `GameplayNotice`. O aviso também é usado na entrada bloqueada da Biblioteca do Campus; marcadores vermelhos não representam mais esse estado. Portas decorativas permanentemente seladas mantêm apenas o retorno sonoro para não sugerir uma chave inexistente.

- `OpenDoorWithKey` sem `requiresKey01`, `requiresKey02` ou `requiresKey03` não abre mais ao entrar no trigger de proximidade. O jogador precisa avançar contra o collider físico da folha; `PlayerMovement.OnControllerColliderHit` então solicita a abertura.
- Após fechar completamente e o jogador deixar o trigger, a porta comum volta a exigir novo contato. Portas com chave, portas de ativação manual, direção da dobradiça, fechamento automático e transições de setor preservam seus fluxos.
- `TCSimpleDoor` com `keyId` vazio segue a mesma regra quando `autoOpen` está habilitado; o antigo raio baseado na câmera não abre mais a porta.
- Inventário estático desta revisão (2026-11-09): 212 componentes `OpenDoorWithKey`, sendo 208 sem chave, 4 com chave e 1 com ativação manual. Compilação em Unity 6000.5.7f1 concluída sem erros; teste de contato em Play Mode não executado.

Validação desta revisão (2026-11-06): revisão estática dos consumidores e do diff. Unity Editor não está disponível neste ambiente; compilação, carregamento das cenas/prefabs, Play Mode, build e comparação visual **não executados**. Na Unity, conferir aproximação por trás com/sem parede, perseguição em curvas, preparação dos ataques, ondas sob observação e após desviar a câmera, pausa durante a espera e liberação do portão do porão após eliminar os sobreviventes. Cobertura do spawn depende dos colisores; superfícies transparentes com colisão e frestas precisam de conferência visual.
