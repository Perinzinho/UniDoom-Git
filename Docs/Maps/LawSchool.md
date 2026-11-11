# Bloco de Direito — LawSchool

Atualização de 09/11/2026: uma chave de acesso à biblioteca sorteada por partida, com sprite em latão e etiqueta ocre. Antigas chaves de cela 1/2/3/9 retiradas do gameplay; chave da manutenção, alarme e suas regras preservados. [Acesso da biblioteca](CampusLibrary.md#acesso-com-duas-chaves--09112026).

Atualizado: **2026-11-08**. Estado: chave sorteada por partida, placas gastas e alarme desligável no quadro da manutenção, preservando visibilidade próxima, mão/arma reativas, correções da manutenção, pias e secretaria. Os resultados de validação de cada rodada estão registrados abaixo. Esta ficha é contexto de manutenção; não é gerador nem substitui inspecionar a versão atual dos assets.

## Leitura rápida

- **Duas chaves independentes — 11/11/2026:** a inicialização aguarda o jogador, posiciona a chave da biblioteca, sincroniza sua disponibilidade pelo ID próprio e publica a colocação inicial para disponibilizar a manutenção no mesmo ciclo. A manutenção compara seu ponto com `SelectedKeyLocation`, conserva a exclusão da sala da biblioteca e exige 45 m de separação horizontal. Cada coleta desativa apenas o próprio objeto e registra seu ID separado no inventário. Cena independente e prefab têm 30 salas elegíveis para a biblioteca e 30 pontos de manutenção, com pelo menos 14 alternativas de manutenção para qualquer escolha da biblioteca. Verificação isolada de 120 casos com os dados desses dois assets e os métodos reais de inicialização/coleta passou nas duas ordens, resolução do alarme e dois retornos ao prédio. Compilação C# isolada passou com avisos preexistentes; Play Mode e inspeção visual pendentes de teste pelo usuário.

- **Acertos das armas — 11/11/2026:** pistola e espingarda usam `WeaponHitQuery` para ignorar triggers de portas/coleta e os colisores do atacante, conservando a primeira obstrução sólida. A linha de visão do machado usa a mesma consulta. O raio compartilhado começa antes do plano de recorte da câmera, mantendo a linha da mira para alvos muito próximos. A física do projeto está configurada para consultas incluírem triggers por padrão; os disparos anteriores herdavam essa configuração. Componentes `Enemy` e cápsulas sólidas das quatro variantes do alarme foram conferidos. Compilação C# do runtime e verificações isoladas com os métodos reais de disparo/impacto passaram; os substitutos de física cobrem gatilhos, colisores próprios, paredes, alcance e alvos mortos. Play Mode, percurso e acertos reais no cenário pendentes de teste pelo usuário.

- **Fonte jogável:** `LawSchool.unity` contém o prédio desembrulhado, independente de `Prefabs/LawSchool.prefab`. Alterar somente esse prefab não altera o mapa carregado pelo jogo. Conferir e corrigir a cena real em mudanças de gameplay.
- Manutenção 104: sem a chave específica, aproximar-se a até 2,5 m do vão exibe **“A porta está trancada”**. O aviso some ao obter a chave, afastar-se ou sair do gameplay; o bloqueio físico continua dependendo da chave. Distância salva tanto na cena independente quanto no prefab.
- Unity **6000.5.7f1**, URP **17.5.0**, Windows/D3D12. Confirme versões nos arquivos do projeto somente se mudaram.
- Interior térreo com hall central, quatro corredores, **36 salas de aula**, manutenção 104 com quadro de energia e ferramentas, quatro banheiros compartilhados entre pares de corredores, secretaria, uma coordenação, copa e auditório. As contagens de revisões anteriores são históricas.
- A ampliação anterior de 2× recebeu **escala uniforme adicional de 1,25× em todo o bloco**, incluindo arquitetura, mobiliário, portas, placas e sanitários. A implantação ocupa aproximadamente **169 m × 100 m**. O prefab do player foi preservado.
- Salas: **13,5 m × 11,75–17 m**, 24–42 cadeiras; seminários e audiências têm arranjos próprios. Carteiras de **0,95 m** de altura e cadeiras de **1,10 m** em mundo, medidas após a calibração.
- Banheiros atuais: **9,5 m ou 13,5 m × 24,5 m**, quatro cabines preservadas e entradas nos dois corredores. Cada banheiro tem **seis pias**, com faces internas e externas; os femininos voltaram às pias de parede, sem o conjunto extra/bancadas de retorno. Três mictórios em cada masculino. Oito superfícies de espelho completas, com material sem reflexão por solicitação posterior. Auditório: **112 lugares**.
- Secretaria junto ao hall, com **entrada de funcionários no corredor 4 e guichê para estudantes no bloco central**, usando a antiga sala 119 da numeração intermediária. Uma coordenação no corredor 2; copa no meio da ala do corredor 1. Antigas salas de serviços, apoio e administração duplicada foram reaproveitadas; a geometria substituída permanece desativada no asset.
- Seleções anteriores: **19 modelos CC0**, com 194 instâncias na primeira e 42 na segunda, antes da redistribuição descrita na revisão do vídeo. As contagens históricas de 228 instâncias ativas e 31 mesas docentes não representam o layout atual. A revisão reaproveita esses modelos, acrescenta o micro-ondas já existente no projeto e redistribui administração, longarinas, audiovisual, bebedouros, lixeiras e segurança. Fontes: `Licenses/AssetSources/LawSchool/{model-replacements.json,equipment-sources.json,video-revision-sources.json}`; pesquisa local em `LawSchool-modelos-referencias.md`, ignorada pelo Git.
- Entrada pelo prédio de Direito no Campus Exterior e pelo menu temporário. Não crie outra cena ou outra opção de menu para o mesmo mapa.
- Suprimentos: nas **36 salas de aula**, uma distribuição por partida reserva **6 salas para medkit, 10 para munição e 20 vazias**. Cada medkit cura até 50; cada pacote adiciona 20 à reserva da arma carregada. Ao voltar ao bloco, salas e itens coletados mantêm o estado da sessão.
- Alarme atual: a primeira chave coletada no próprio prédio, entre acesso à biblioteca e manutenção, inicia sirene e alterna fases vermelhas/apagadas. O disparo é único por sessão; após desligar o quadro, nenhuma outra chave reinicia o evento. Pool de 30 ambientes, fora da ala da manutenção e das salas reservadas 110/121/132. Até **34 zumbis variados** chegam, com intervalo de 0,55 s e regras de spawn seguro preservadas. A chave da manutenção existe desde a entrada, em outra sala a pelo menos 45 m. A sala 104 exige essa chave; o quadro exige apenas a chave da manutenção, permitindo desligar mesmo sem a chave da cela 1. Ao desligar o quadro, a saída para o campus é liberada, sem remover zumbis presentes. Lanterna durante o alarme alternada em F/direcional para cima.

- Chaves atuais: **acesso à biblioteca + manutenção**. Antigas celas 1/2/3/9 desativadas ou convertidas, sem transferência para outro mapa. Os pontos do sorteio e a separação da manutenção foram preservados. A cena jogável independente e o prefab receberam a alteração.
- Atualização atual do alarme: **34 chegadas**, redução de aproximadamente 10% sobre as 38 anteriores. Chave da manutenção com sprite exclusivo de metal gasto e plaqueta amarela com chave de boca, fundo transparente e altura visível de 24 cm. Aplicado na cena jogável e no prefab independente.

## Pedido do usuário e decisões aceitas

Criação: usar `.codex/reference/direito reference.png`, ligar o interior ao prédio do campus Unimar, incluir no menu temporário, aplicar a skill Total Chaos e o prompt mestre, otimizar e usar modelos 3D gratuitos compatíveis.

Variação: cada sala deve ter uma peculiaridade — lotação, carteiras, lousa lateral/oposta à porta, equipamentos, material de estudo ou situação de abandono.

Correção posterior, que **prevalece sobre o layout compacto anterior**:

> Faça o mapa bem maior [...] as salas devem parecer igual as de faculdade, o mesmo vale para os banheiros [...] levando em consideração o tamanho do player [...] o bloco precisa parecer um bloco de direito de uma faculdade [...] estilo mais assustador [...] semelhante a total chaos.

O usuário autorizou ampliar o interior e depois pediu que **absolutamente tudo do mapa** tivesse proporção equivalente ao prefab do player, citando carteiras que pareciam infantis pela câmera. A revisão de proporções é aplicada à raiz do edifício, com ajuste das chegadas e das luzes. Não reverta a escala 1,25 nem redimensione o player. O traçado externo do campus permanece preservado. Não volte às salas iniciais de 5,4 m de largura e 4–16 cadeiras. Salas administrativas podem ter poucas mesas; diferencie-as de salas de aula pelo uso e pela sinalização.

## Arquivos e identidade

Passos por piso (2026-11-06): o controlador global `FootstepController` agora resolve os suportes `*floor collision` desta cena como piso interno (`Eco`) e `Low stage collision`/`Stage step collision` como madeira. Não depende de renderer nos colisores; `FootstepSurface` permite sobrescrever a escolha em pisos futuros. Prefab, layout e materiais não foram modificados. Compilação, Play Mode e escuta no mapa ainda não executados: Unity indisponível no ambiente desta alteração.

Prefixo exclusivo: `Assets/_Project/Scenes/LawSchool/`.

| Conteúdo | Caminho relativo ao projeto |
|---|---|
| Cena jogável | `Assets/_Project/Scenes/LawSchool/LawSchool.unity` |
| Prefab independente, não instanciado na cena jogável | `Assets/_Project/Scenes/LawSchool/Prefabs/LawSchool.prefab` |
| Materiais e meshes persistentes | `Assets/_Project/Scenes/LawSchool/Materials/` e `Meshes/` |
| Mobiliário escolar | `Prefabs/SchoolChair_01.prefab`, `Prefabs/SchoolDesk_01.prefab` sob o prefixo |
| Sanitários | `Models/Sanitary/`, `Textures/Sanitary/`, `Prefabs/Sanitary_*.prefab` sob o prefixo |
| Iluminação indireta necessária | `Assets/_Project/Scenes/LawSchool/Scripts/LawSchoolLighting.cs` |
| Evento de alarme e coleta | `Assets/_Project/Scenes/LawSchool/Scripts/LawSchoolAlarmPrototype.cs`, `LawSchoolCellKeyPickup.cs` |
| Quadro interativo e desgaste das placas | `Assets/_Project/Scenes/LawSchool/Scripts/LawSchoolPowerPanel.cs`; `Shaders/WornSignUnlit.shader` sob o prefixo da cena |
| Bloqueio e aviso da saída | `Assets/_Project/Scenes/LawSchool/Scripts/LawSchoolExitLock.cs`; guard opcional em `Global/Scripts/Utils/SceneTransitionDoor.cs` |
| Carregamento exclusivo do bloco | `Resources/Textures/UI/Transitions/LawSchool.png` sob o prefixo do mapa; seleção em `Global/Scripts/UI/DoorReloadScreen.cs` |
| Inventário de chaves específicas | `Assets/_Project/Global/Scripts/Player/PlayerCellKeys.cs` — adicionado ao player na coleta; não usa os flags das chaves antigas |
| Inimigo e som do protótipo | `Assets/_Project/Scenes/LawSchool/Prefabs/LawSchoolAlarmZombie.prefab`, `Audio/LawSchoolAlarm-Prototype.wav` |
| Sprite compartilhado de chave | `Assets/_Project/Global/Textures/Pickups/PrisonKey.png` — movido por Unity API, mesmo GUID/importador |
| Pós-processamento | `Assets/_Project/Scenes/LawSchool/Profiles/LawSchool.asset` |
| Oclusão nativa | `Assets/_Project/Scenes/LawSchool/LawSchool/OcclusionCullingData.asset` |
| Referência enviada pelo usuário | `.codex/reference/direito reference.png` |
| Menu compartilhado | `Assets/_Project/Global/Scripts/UI/GameMenus.cs` |
| Sessão e criação do player | `Assets/_Project/Global/Scripts/Core/GameManager.cs`; raiz `SpawnPoint` da cena |
| Cenas habilitadas | `ProjectSettings/EditorBuildSettings.asset`; conferir também o Build Profile ativo |
| Fontes, licenças, hashes | `Licenses/AssetSources/LawSchool/sources.json`, `sanitary-sources.json`, `sanitary-readme.txt`, `model-replacements.json`, `equipment-sources.json` |

GUIDs úteis: cena `4c02879dcd5ae9c41bbb62dae57b8451`; prefab principal `8f2dc05494d3c2b43ac0531067ae8583`; oclusão `58b8f20070a67144091fb120fea2ba75`; cadeira `54c99ad2d5a2e8f429d60d024844acbe`; carteira `2f9f62908021eda41b3812a84cea2326`.

A pasta aninhada `LawSchool/LawSchool/` é usada pela Unity para a oclusão; não reorganize só pelo nome repetido. Não renumere fileIDs. Alterações da cena e do prefab devem permanecer coerentes.

`Meshes/Part_*`, `Finish_*`, `FixtureReview_*`, `University_*` e `Atmosphere_*` são resultados persistentes referenciados pelo mapa. O prefixo de geração não os torna descartáveis; verifique dependências antes de qualquer limpeza.

## Layout, escala e circulação

- Raiz: `Bloco de Direito — pavimento único`; numeração **101–137**, alternando as portas dos dois lados em ordem de afastamento do hall, com **104 reservada à manutenção**. Corredor 2: **101–109**; corredor 1: **110–118**; corredor 4: **119–127**; corredor 3: **128–137**. Secretaria e copa não recebem número de sala de aula. A conversão da 104 mantém os outros números estáveis.
- Hall no eixo X=0; centros dos corredores em **Z=21,5 e Z=54**, nos dois lados. Auditório após **Z=75**. A raiz do prédio e do prefab possui escala uniforme **(1,25; 1,25; 1,25)**. Os dados locais anteriores continuam serializados; converta-os para mundo antes de comparar dimensões.
- Altura livre aproximada: **4,06 m**. Vãos nominais de salas: aproximadamente **1,75 m**, com batentes; altura nominal de passagem próxima de **3,075 m**. Preserve o piso, as junções e a abertura efetiva da folha.
- Player: `Assets/_Project/Global/Prefabs/Player/Player.prefab`; escala da raiz `(0,4733; 2,1210; 0,4197)`. A cápsula resulta em **2,121 m de altura e 0,473 m de diâmetro**; a câmera fica aproximadamente **1,839 m acima dos pés**. Meça o resultado em mundo, não só `CharacterController.height` local. **Não redimensione o player para fazer a sala caber.**
- Cadeira escolar **1,10 m**; carteira **0,95 m** de altura em mundo. Toda a geometria anterior, incluindo bancadas, cubas, vasos, auditório, portas, placas e respectivos colisores, acompanha a escala uniforme. Mesas docentes e administrativas têm tampos maiores; escritórios usam cadeiras estofadas e cadeira de trabalho, em vez de uma carteira escolar isolada. Confira contato com piso/tampo e o espaço de uso.
- Fileiras têm passagem central e laterais; lousas laterais exigem orientação correspondente das carteiras. Seminários circulares e audiências simuladas são variações intencionais.
- Os 40 ambientes das alas possuem porta por proximidade com `OpenDoorWithKey`, sem exigir chave. A sala 127 recebeu a folha que faltava, e os vãos/batentes foram ajustados à folha real. Não presumir que apenas a existência do transform da porta garante uma folha funcional.

## Conexões

| Origem | Destino/ponto | Posição em mundo |
|---|---|---|
| Prédio `04A Law school exterior` no campus | `LawSchool` / `FromCampus` | Entrada externa aprox. `(-43; 1,85; 14,4)` |
| Chegada no interior | `FromCampus` | `(0; 1,12; 5,25)`; olhando para o hall; a altura da cápsula não foi multiplicada |
| Saída do hall | `CampusExterior` / `FromLawSchool` | Trigger perto de `(0; 1,5625; 0,625)`; folhas e painéis opacos completos atrás do volume |
| Chegada de volta ao campus | `FromLawSchool` | `(-43; 1,75; 12,40)`; yaw 180° |

Portais: `Assets/_Project/Global/Scripts/Utils/SceneTransitionDoor.cs`; chegadas: `SceneArrivalPoint.cs` na mesma pasta. Cena e prefab do campus ficam em `Assets/_Project/Scenes/CampusExterior/`. GUID da cena do campus: `faaefa97714810b429c19b5065aff68d`.

No exterior, o pátio entre o Bloco de Direito e a biblioteca possui a instância `Ornate fountain - Law School and Library` em `(-58; 0; 16)`, além de bancos de concreto. Preserve a circulação entre esse mobiliário, a entrada do bloco e a entrada da biblioteca ao revisar conexões.

Menu: botão `TemporaryMapSelect` → `Map_LawSchool`; rótulo **BLOCO DE DIREITO**. A lista deriva das cenas habilitadas. Ao verificar TMP de botão, procure o texto nos filhos; o primeiro TMP pode ser a seta de seleção.

O `spawnPoint` serializado do `GameManager` aponta para a raiz `SpawnPoint` deste interior. Preserve serviços, câmera e HUD compartilhados; não copie outro manager mantendo o spawn de outro mapa.

## Direção visual e assets

Norma: `.codex/SKILL.md`, seções Geometria e escala, Texturas e materiais, Iluminação, Composição e narrativa e Checklist. Referência: **mod clássico Total Chaos no ModDB**, não presumir técnicas do remake nem copiar seus assets.

Madeira envelhecida, reboco frio, piso gasto, sinalização azul e fluorescentes definem a universidade. Desgaste concentra-se em infiltrações, rodapés e zonas de uso; há forro modular, luzes mortas, documentos e alguns móveis deslocados. Preserve leitura de portas, obstáculos e sinalização; não resolva o horror enterrando a cena em preto.

Autores/fontes principais: **Ethan Place / Poly Haven**, cadeiras e carteiras; **loafbrr_1 / OpenGameArt**, vasos, cubas e mictórios. Links e licenças estão em [Referencias-e-Assets.md](Referencias-e-Assets.md). Texturas compartilhadas reaproveitadas: `Assets/_Project/Global/Surface/ClassRoom/Textures/` e reboco em `Assets/_Project/Global/Textures/Library/DecayRevision/damaged_plaster/`. Planta reaproveitada de `Assets/_Project/Scenes/EntranceHallway/Prefabs/CampusReference/CampusPottedPlant.prefab`.

PBR: texturas sanitárias e escolares em 1K; normal maps importados como normal, máscaras em espaço linear. ARM/AoRM destes assets foram convertidos para Unity Lit: **R=metallic, G=occlusion, A=1−roughness**. Os manifestos conservam hash de origem/conversão. Não converta novamente uma máscara já convertida.

Compartilhamento com CampusLibrary, 07/11/2026: os meshes de vaso/pia e seus materiais/texturas utilizados passaram para `Global/{Meshes,Materials,Textures}/Sanitary/`; o prefab de extintor passou para `Global/Prefabs/Equipment/korean_fire_extinguisher_01.prefab`. Os 19 assets foram movidos pela Unity API, com GUIDs e conteúdo preservados; os prefabs existentes de LawSchool continuam referenciando os mesmos recursos. Prefabs sanitários e FBX de origem permanecem em LawSchool. Lista exata, fontes e SHA-256: `Licenses/AssetSources/CampusLibrary/reused-props-sources.json`.

Iluminação: `LawSchoolLighting` mantém ambiente/fog consistentes ao trocar de cena com câmera persistente. Luzes locais sem sombras na maioria; fontes selecionadas usam sombra. No PC, o tier **Medium do UniversalAdditionalLightData é 512 px**; o atlas é 2048. Em URP, alterar só `Light.shadowResolution` não substitui conferir o tier adicional. Não altere configurações globais para resolver apenas este mapa.

## Revisão de fechamento, ambientes e proporções

### Orientação das pias e suprimentos nas salas — 06/11/2026

- Corrigidas as **24 pias ativas**, seis em cada banheiro: yaw local **90°**, com torneiras junto à parede e frente fechada das cubas voltada para a circulação. Escala, posições, materiais, mesh de duas faces, bancadas masculinas e pias femininas de parede preservados. Medida dos vértices superiores confirmou o lado das torneiras em direção à parede. Os quatro banheiros permanecem ativos; sinks têm flags estáticas 0, sem alteração dos oclusores do prédio.
- `LawSchoolRoomSupplies` usa 36 pontos persistentes nas salas de aula, afastados pelo menos 1,4 m em planta do marcador da chave e conferidos contra móveis, piso e aproximação da cápsula do jogador. **6 medkits / 10 munições / 20 salas vazias**, com no máximo um suprimento por sala. Salas de serviço e banheiros não participam desta distribuição.
- `LawSchoolSupplyState` é adicionado ao player persistente, guardando a distribuição sorteada e os itens já coletados. Atribuições permanecem iguais ao sair/voltar ao bloco; itens consumidos não são repostos. Uma nova sessão cria outra distribuição. Estado em memória da partida, sem novo formato de save em disco.
- Prefabs locais `LawSchoolRoomMedkit` e `LawSchoolRoomAmmo` reutilizam os prefabs, sprites, materiais, áudio e coleta globais. Ícones com **0,45 m e 0,30 m de largura**, respectivamente, e triggers de **0,65 × 0,80 × 0,65 m**. A instanciação preserva escala de mundo 1 apesar da raiz 1,25 do mapa. Medkit cura até **50**; munição adiciona **20** à reserva da arma carregada, sem preencher automaticamente o carregador.
- `MedkitPickup` e `AmmoPickup` receberam somente o evento `Collected`, emitido após uma coleta bem-sucedida. `LawSchoolSupplyPickup` registra esse evento no estado da sala; não muda a regra de cura/munição nem considera descarregar a cena uma coleta. Os prefabs globais e o prefab do player foram preservados por SHA256.
- O alarme agora registra sprites preexistentes do prédio na mesma apresentação dos inimigos: os suprimentos ficam vermelhos e, durante o apagão, fracamente visíveis até 4,5 m. Desligar o painel restaura suas cores. Geometria e placas continuam no tratamento anterior; a horda de 60 e a exclusão do corredor 2 no sorteio da chave foram preservadas.
- Play Mode: distribuição e **24 sorteios independentes** conferidos; todos os 16 itens têm suporte físico e tamanho adequado. Coleta real pelo CharacterController confirmou **+50 de vida** e **+20 de reserva**, uma única vez; medkit com vida cheia foi recusado. Recarregar a cena manteve as 36 atribuições e deixou **5 medkits e 9 munições**, sem repor os dois itens coletados. Vermelho/apagão, painel e restauração das cores passaram com **zero erros runtime**. Capturas: `output/Direito-Pias-Corrigidas.png` e `Direito-Suprimentos-{Medkit,Municao}.png`. Evidências em `.local/LawSchoolAlarm/supplies-{config.txt,play.txt,identity.json}`. Não houve novo bake de oclusão nem benchmark de FPS nesta rodada.

- Identidade: preservados todos os **25.827 fileIDs anteriores da cena e 25.764 do prefab**, com 75 novos em cada asset. Prefabs globais de player/medkit/munição e prefab base da pia conservam os hashes. Build Windows Development de LawSchool + CampusExterior: **Succeeded, zero erros, oito avisos, 44,41 s**; avisos CS0618 e nome `StreetParkingBays`, fora das alterações desta rodada.

### Horda variada de 60 e chave fora da ala de manutenção — 06/11/2026

- Alarme com **até 60 chegadas**: **36 no hall principal e 24 nos quatro corredores**. Os 32 marcadores antigos foram preservados; 24 continuam na programação e os demais não são usados nesta leva. Acrescentados 36 marcadores centrais, intercalados com os corredores. Três pontos foram deslocados para evitar mobiliário e acomodar o cadeirante. Todos os 60 passaram na verificação física das quatro cápsulas; intervalo **0,55 s** mantido.
- Reutilizados os **quatro tipos existentes**, em ciclos equilibrados com início sorteado: comum, mulher, cadeirante e fã que arremessa objetos. Leva completa: **15 de cada**. Os três novos prefabs `LawSchoolAlarm{Woman,Wheelchair,Fan}` são variantes locais do `LawSchoolAlarmZombie`, referenciando `Global/Prefabs/Enemy/{Woman,Wheelchair,Fan}ZombieAppearance.asset`; nenhum sprite, textura ou código de IA foi duplicado. Assets globais de aparência, prefab global de inimigo e prefab do player preservados por SHA256.
- `LawSchoolAlarmPrototype.zombieVariants` seleciona as variantes; a checagem física considera altura, raio e centro próprios de cada cápsula. Aparições usam `EnemySpawnSafety` para exigir cobertura do corpo inteiro quando dentro do enquadramento, ignorando outros zumbis como cobertura e respeitando a distância mínima de **10 m**. Escala de mundo 1 e posição dos pés conferidas nas quatro variantes.
- A manutenção fica em **Ala — corredor 2**. `excludedKeyAreas` referencia essa ala e os dois banheiros compartilhados entre os corredores 2/1. `keyLocations` conserva **33 ambientes permitidos**; marcadores antigos e **todos os quatro banheiros físicos permanecem ativos**. O filtro também é aplicado no runtime. `PlayerCellKeys.GetOrChooseKeyLocation` conserva uma escolha memorizada somente se ela continuar permitida, evitando reutilizar um local do corredor excluído ao retornar à cena.
- Play Mode: escolha antiga proibida substituída, **64 sorteios novos** dentro dos ambientes válidos, coleta real, **60 zumbis vivos em aproximadamente 34,93 s**, contagem **36/24**, quatro grupos de 15, fases vermelha/apagão, escala/pés, tecla **E**, fim da sirene, chave conservada, saída física liberada e os 60 inimigos mantidos. **Zero erros runtime**. Prévia 3D em `output/Direito-Horda-60-Hall.png`; a câmera foi posicionada temporariamente no hall com a simulação pausada para inspeção visual, sem alterar o mapa. Evidências em `.local/LawSchoolAlarm/mixed60-{config.txt,play.txt,identity.json}`. Não foi feito benchmark de FPS nem novo bake de oclusão: os novos marcadores não têm renderizadores.
- Conservados os **25.755 fileIDs anteriores da cena e 25.692 do prefab**; adicionados 36 objetos de chegada com seus transforms em cada asset. Build Windows Development de LawSchool + CampusExterior: **Succeeded, zero erros, oito avisos, 33,41 s**. Avisos CS0618 e de nome do asset `StreetParkingBays` do campus, sem alterações nesses consumidores nesta tarefa.

### Carregamento exclusivo, horda e sirene — 06/11/2026

- **Até 32 zumbis**, oito pontos por corredor. Os 12 anteriores foram preservados e receberam mais 20 posições livres, intercaladas entre as quatro alas. Intervalo reduzido de **1,2 s para 0,55 s**, mantendo início após 2 s, distância mínima de 10 m, verificação de espaço/visada e limite finito. Todos os 32 pontos passaram na checagem de cápsula; não houve alteração de dano, velocidade ou IA compartilhada.
- Sirene com **volume 0,85** (antes 0,22) e **prioridade 16**. O WAV original de 2 s, mono/22.050 Hz/PCM16, teve o pico elevado de **0,2514 para 0,65**, sem saturar o arquivo; ganho combinado de aproximadamente **20 dB** em relação à configuração anterior. Importador, GUID e controle geral de volume preservados. O painel continua parando a sirene.
- Play Mode confirmou **32 zumbis vivos**, chegadas graduais completadas em aproximadamente **19,53 s**, áudio tocando com os novos parâmetros, fases vermelha/apagão de **1,6 s**, uso real de **E** no quadro, alavanca, conservação da chave e liberação da saída: **zero erros runtime**. O teste passou a retomar a pausa automática por perda de foco durante a automação; o comportamento de pausa do jogo não foi alterado. Evidências locais `horde-{config.txt,audio.txt,play.txt}` em `.local/LawSchoolAlarm`.
- Carregamento personalizado usa **captura original do hall real em 1920 × 1080**, seguindo a abordagem da `CampusLibrary`, com título **BLOCO DE DIREITO**, vinheta, pulsos e ambiente sonoro do `DoorReloadScreen` existente. Asset: `Assets/_Project/Scenes/LawSchool/Resources/Textures/UI/Transitions/LawSchool.png`. Captura sem HUD, câmera em y = 1,898 m no mundo, FOV 65°, posição local `(-1,7; 1,5184; 5,8)`, mirando `(0,6; 1,5; 43)`. Importação sem redimensionamento NPOT e sem mipmaps preserva o formato **16:9**. Não foram gerados novos objetos no mapa para a imagem.
- `DoorReloadScreen` reconhece `LawSchool` por cena, o nome no checkpoint e os aliases de prévia **DIREITO / BLOCO DE DIREITO**, incluindo o ciclo F8. O destino desconhecido continua usando a arte compartilhada. Entrada real **CampusExterior → LawSchool / FromCampus** aprovada em Play Mode, com arte/título corretos, cobertura removida e tempo de jogo restaurado: **zero erros runtime**. A captura de teste mantém a cobertura por 4 s somente na instância de Play Mode; a duração padrão do produto foi preservada.
- Conferência da entrada repetida após preservar a proporção da imagem, novamente aprovada com zero erros runtime. Prévia final: `output/Direito-Tela-Carregamento.png`. Preservados os **25.715 fileIDs anteriores da cena e 25.652 do prefab**; acrescentados somente 20 pontos com seus transforms em cada um. Prefab do player preservado por SHA256. Sem novo bake de oclusão nesta rodada: os pontos não têm renderizadores e a imagem é uma captura; não foi feito benchmark de FPS.
- Build final Windows Development de **LawSchool + CampusExterior**: **Succeeded, zero erros, oito avisos, 33,13 s**. Relatório em `.local/LawSchoolAlarm/build.txt`; avisos são os CS0618 dos sistemas compartilhados e o nome do asset `StreetParkingBays` do campus, fora deste ajuste.

### Mais zumbis durante o alarme — 06/11/2026

- Leva aumentada de **6 para até 12 zumbis**. Mantidos os seis pontos existentes e acrescentados seis, totalizando **três por corredor**, na cena e no prefab. Novas posições locais: `(-58; 0,868; 17,2)`, `(-60; 0,868; 43,2)`, `(42; 0,868; 17,2)`, `(58; 0,868; 17,2)`, `(43; 0,868; 43,2)` e `(60; 0,868; 43,2)`.
- Mantidos intervalo de **1,2 s**, início após o vermelho inicial, distância mínima de **10 m**, verificação de cápsula e proteção contra aparecer na visão aberta do jogador. Desligar o quadro cancela chegadas pendentes e conserva os zumbis já presentes. Não há geração infinita.
- Os **12 pontos passaram na verificação de espaço físico**. Play Mode com coleta real da chave confirmou **12 zumbis vivos**, chegadas graduais ao longo de aproximadamente **16,55 s**, câmera e painel nas duas fases, tecla **E**, alavanca desligada, chave conservada e saída liberada: **zero erros runtime**. A duração do apagão permanece **1,6 s**. Evidências locais em `.local/LawSchoolAlarm/{wave-config.txt,wave-identity.json,panel-play.txt}`.
- Preservados todos os **25.703 fileIDs da cena e 25.640 do prefab** anteriores ao aumento; acrescentados seis objetos de chegada com seus transforms em cada asset. Prefab do player preservado por SHA256. Build Windows Development de LawSchool + CampusExterior: **Succeeded, zero erros, oito avisos, 55,52 s**. O relatório inclui aviso de nome do asset `StreetParkingBays` do campus; esse asset não foi alterado nesta tarefa. Oclusão preservada nesta etapa, pois os novos pontos não possuem renderizadores nem alteram o layout.

### Painel elétrico e aproximação da câmera — 06/11/2026

- Reproduzida a tela azulada com a câmera real do player: o gabinete estava voltado para a parede, e sua face metálica traseira preenchia a tela ao encostar. A comparação da oclusão e seis ângulos confirmou o ambiente ao redor; o problema reproduzido era enquadramento/posicionamento do prop, não uma transição para outro mapa.
- Reaproveitado o gabinete **Poly Haven `power_box_01`**, com materiais PBR, tampa aberta e dobradiças originais. Orientação corrigida para dentro da manutenção, corpo alinhado à parede sem enterrar a tampa, colisão própria na folha e volume frontal ajustado. Corpo com aproximadamente **0,92 × 1,35 m**, base a **1 m** do piso; raiz local **(-5,13; 0,80; 5)** com yaw **90°**, preservando a escala 1,25 do edifício.
- Acrescentados **12 disjuntores**, trilhos, bornes, peças com bordas arredondadas, dois conduítes com abraçadeiras e **chave geral vermelha**. Identificação, circuitos e avisos em português fazem parte da textura da chapa, com material iluminado pela cena. Metal com brilho reduzido. Reutilizados aço, latão e pintura vermelha existentes; novos assets exclusivos permanecem nas pastas `Meshes/Maintenance`, `Textures/Maintenance/ControlPanel` e `Materials/Maintenance`.
- `LawSchoolPowerPanel.mainSwitch` aponta para o pivô do comando: usar **E** desliga o alarme e move a alavanca para a posição desligada. Alcance de 3 m, raycast, pausa/morte, chave específica, fases do alarme e bloqueio físico da saída preservados. Fonte e hashes atualizados em `Licenses/AssetSources/LawSchool/maintenance-sources.json`.
- Play Mode com **Cinemachine e câmera reais**, sem desativar a oclusão: 50 movimentos contra o painel, seis ângulos, coleta real da chave, vermelho e apagão **1,6 s**, prompt, tecla **E**, alavanca desligada, restauração da iluminação, chave conservada e liberação da colisão da saída. **Zero erros runtime**. Evidências locais em `.local/LawSchoolAlarm/panel-play.txt` e capturas `panel-*`. Não foi feito benchmark de FPS.
- Oclusão recalculada após os ajustes. Build Windows Development de LawSchool + CampusExterior: **Succeeded, zero erros, sete avisos CS0618 preexistentes, 49,33 s**. Preservados todos os **25.702 fileIDs da cena e 25.639 do prefab** anteriores a esta rodada; nova referência ao pivô acrescentada em cada um. Peças persistentes ficam no prefab de manutenção reaproveitado. Prefab do player preservado por SHA256.
- Conferência completa de Play Mode repetida **após o bake**, novamente aprovada com zero erros runtime. Prévias finais com HUD em `output/Direito-Painel-Revisado.png` e `output/Direito-Painel-Apagao.png`.

### Duração do apagão — 06/11/2026

- Reduzido de **2 s para 1,6 s** (20%): valor padrão do script e valores serializados na cena e no prefab. Vermelho inicial de 2 s e vermelho recorrente de 3 s preservados. Conferência estática confirmou que só o valor do apagão mudou nesses três arquivos; não foi executado novo Play Mode ou build para este ajuste de duração.

### Colisão física da saída durante o alarme — 06/11/2026

- Corrigida a abertura central, que tinha somente o trigger da transição: as colisões sólidas existentes cobriam apenas os painéis laterais. O bloqueio anterior impedia trocar de cena, mas seu teste de permanência no trigger não verificava atravessar todo o vão.
- Cena e prefab agora têm um `BoxCollider` sólido independente, filho da saída, com **6,10 × 4,25 × 0,40 m** em mundo, centrado em **(0; 2; 0)** e sobreposição nas laterais. O trigger original continua intacto. `LawSchoolAlarmPrototype.StateChanged` ativa a barreira imediatamente ao iniciar o alarme e a remove ao resolver o evento no quadro; `LawSchoolExitLock` mantém esse estado também durante apagões e pausa.
- As folhas existentes da entrada são estáticas, sem controlador de abertura. Permanecem fechadas e imóveis durante o alarme; o guard continua impedindo a transição e a tela de reload. As animações das portas das salas não foram alteradas.
- Play Mode: coleta real da chave, **572 movimentos do CharacterController**, com **6 s de caminhada contínua em cada faixa esquerda/centro/direita**, cobrindo vermelho e apagão. A cápsula parou em **z = 0,49 m**, com os pés em aproximadamente **y = 0,08 m**, sem atravessar, cair, animar a entrada ou iniciar carregamento. Aviso e bloqueio na pausa conferidos. A interação de produção do painel removeu a colisão; atravessar depois carregou **CampusExterior / FromLawSchool**. **Zero erros runtime**.
- Preservados os **25.699 fileIDs anteriores da cena e 25.636 do prefab**; adicionados somente objeto, transform e colisor da barreira em cada asset. Prefab do player e escala 1,25 do edifício preservados. Evidências locais: `.local/LawSchoolAlarm/physical-exit-{play.txt,identity.json}`. Sem alteração de renderizadores ou oclusores, portanto não houve nova geração de oclusão nem benchmark de FPS.
- Build Windows Development de LawSchool + CampusExterior: **Succeeded, zero erros e sete avisos CS0618 preexistentes**. Checklist atualizado com os dois ajustes físicos concluídos.

### Bloqueio inicial da transição durante o alarme — 06/11/2026

- `LawSchoolExitLock` foi ligado à saída existente `Saída para o campus`, na cena e no prefab, sem alterar destino **CampusExterior / FromLawSchool**, volume, folhas ou colisões. Enquanto `LawSchoolAlarmPrototype.IsActive` é verdadeiro, o portal recusa a transição tanto ao entrar quanto ao permanecer no trigger. O estado é consultado diretamente ao tentar sair, sem depender da ordem de `Update`.
- `SceneTransitionDoor` recebeu um guard opcional (`ISceneTransitionGuard`); portais sem guard mantêm o comportamento anterior. Somente a saída de LawSchool foi vinculada ao novo componente.
- Ao se aproximar até **3 m dos limites do volume da porta**, o jogador vê na HUD: **“SAÍDA BLOQUEADA PELO ALARME / Desligue o alarme no quadro de energia da manutenção.”** O aviso some ao se afastar, pausar, morrer ou desligar o alarme. Não exige olhar diretamente para a porta.
- Antes da coleta e depois de usar o quadro da manutenção, a saída permite sair normalmente. As verificações anteriores de chegada, chaves, vida e carregamento continuam em vigor. A geometria estática não foi modificada, portanto a oclusão foi preservada.
- Play Mode com player real: saída liberada e aviso ausente antes da coleta; bloqueio imediato ao coletar a chave; aviso perto e oculto longe/na pausa; entrada e permanência de **1 s dentro do trigger com alarme ativo sem iniciar carregamento**; uso da interação de produção do quadro liberou o portal; atravessar o mesmo trigger depois carregou **CampusExterior** de fato. Zero erros runtime. A tecla E do painel não foi alterada e seu teste de entrada permanece registrado na rodada anterior.
- Conservados os **25.698 fileIDs anteriores da cena e 25.635 do prefab**; acrescentado um componente de bloqueio em cada asset. Prévias em `output/Direito-Saida-{Bloqueada,Liberada}.png`; evidências locais `exit-play.txt`, `exit-config.txt` e `exit-identity.json` em `.local/LawSchoolAlarm/`. O checklist passa a **68 itens preenchidos concluídos**.
- Build Windows Development de LawSchool + CampusExterior: **Succeeded, zero erros, sete avisos CS0618 preexistentes, 34,47 s**. Ferramentas temporárias verificadas e arquivadas fora de `Assets`; nenhuma dependência runtime delas. Não houve novo benchmark de FPS nem uma rodada de testes dos demais mapas; seus portais mantêm guard nulo por padrão.

### Sorteio da chave e conclusão no painel — 06/11/2026

- `LawSchoolAlarmPrototype.keyLocations`: **44 pontos**, um por ambiente: 36 aulas, coordenação, secretaria, copa, quatro sanitários e auditório. Manutenção excluída tanto na preparação quanto na seleção runtime. Pontos sobre mesas ficam perto de uma borda alcançável; os demais usam piso com espaço para a cápsula real. Um único objeto da chave é reposicionado ao começar o mapa.
- `PlayerCellKeys` conserva a escolha e os eventos concluídos no player persistente. `GetOrChooseKeyLocation` sorteia somente uma vez por chave/sessão; a preferência `UniDoom.LastCellKeyRoom.professor-cell-01` armazena apenas o último ambiente para evitar repetição entre partidas consecutivas. Não salva o inventário em disco nem converte a chave em universal. Seleção pode ser consultada no getter runtime `SelectedKeyRoom`, sem marcador revelador na HUD.
- `LawSchoolPowerPanel` no modelo `power_box_01`: interação física com raycast até **3 m**, **E** no teclado ou botão oeste do gamepad, bloqueada durante pausa/morte e antes do evento. Prompt aparece na HUD somente quando é possível usar o quadro. A operação registra `law-school-cell-key-alarm` como concluído na sessão, para a sirene, restaura iluminação/materiais/HUD e cancela chegadas pendentes. Não elimina os inimigos já presentes; chave permanece recolhida. Reentrada conserva o evento resolvido.
- Placas: shader URP opaco `UniDoom/Environment/Worn Sign Unlit`, mantendo textura impressa original, profundidade e normais; desgaste procedural apaga partes da tinta e reduz contraste sem gerar cópias dos bitmaps. **48 superfícies**: 40 identificações de ambientes e oito faces das quatro placas de corredor. Desgaste mais forte nas salas (0,82) e mais leve nos corredores (0,55). Materiais derivados em `Materials/Signage/Worn/`; shader incluído no controle de emissivos/unlit do alarme, portanto as placas continuam apagadas no escuro. Não houve benchmark de GPU ou alegação de ganho de FPS.
- Validação do fluxo: **128 sorteios de novas partidas** sem repetição consecutiva, escolha estável na mesma sessão, cobertura de 44 ambientes e exclusão da manutenção. No Play Mode, coleta real da chave sorteada, alarme/HUD/visão próxima/pausa, rejeição do painel à distância e desligamento pela tecla **E real do Input System** passaram. Após recarregar a cena de fato, local sorteado, chave recolhida e alarme concluído foram preservados. Zero erros runtime. O teste restaura a preferência de sala anterior ao terminar.
- Build Windows Development de LawSchool + CampusExterior: **Succeeded, zero erros, sete avisos CS0618 preexistentes, 39,15 s**, incluindo o novo shader. Oclusão da revisão estrutural anterior preservada, pois esta etapa não altera paredes ou passagens. Conservados todos os **25.608 fileIDs anteriores da cena e 25.545 do prefab**. Ferramentas/evidências locais em `.local/LawSchoolAlarm/`, prefixo `flow-`; ferramentas arquivadas fora de `Assets` após conferir dependências. Sem novo benchmark de FPS. O checksum atual está em `flow-identity.json` local.
- Checklist local: **66 itens preenchidos concluídos**. Capturas finais em `output/Direito-Fluxo-{Placas-Gastas,Chave-Sorteada,Painel-Interacao,Painel-Desligado}.png`, além das capturas `Direito-Ajustes-*` da revisão anterior. Última repetição de validação: 128 sorteios passaram pelos 44 ambientes, sem manutenção ou repetição consecutiva; coleta física, tecla E e recarga real passaram novamente com zero erros.

### Ajustes após o protótipo — 06/11/2026

- **Manutenção:** o grupo `Standard sealed doorway` estava desativado após converter a antiga aula 104, deixando faltar a parede frontal. O grupo foi reativado e a união estrutural foi regenerada no mesmo asset/GUID, agora com **351 volumes, 6.860 vértices e 3.430 triângulos**. Render e MeshCollider correspondem; raio por dentro encontra a parede frontal a 7,25 m. A porta continua abrindo por proximidade e foi atravessada com o player real.
- Cinco modelos novos da Poly Haven, **CC0 e PBR 1K**: `power_box_01` (Rico Cilliers; rig de Yann Kervran), `metal_toolbox` (Mateusz Sadek), `tool_cart` (Savva Zakharov), `flathead_screwdriver` (Dylan Detwiller) e `wooden_hammer_01` (James Ray Cock). Fontes, autores, downloads, hashes e conversões em `Licenses/AssetSources/LawSchool/maintenance-sources.json`; pesquisa local em `LawSchool-modelos-referencias.md`, ignorada pelo Git. Geometria rigada do quadro salva como meshes estáticos; máscara ARM convertida para URP. Mesa metálica e estantes CC0 existentes reaproveitadas como bancada/armazenamento. Exclusivos em `{Models,Textures,Materials,Meshes,Prefabs}/Maintenance/` sob o prefixo da cena.
- **Pias:** a malha original tinha superfícies cerâmicas voltadas para dentro, permitindo enxergar o interior pelo lado externo. `Meshes/Sanitary_Sink_A_TwoSided.asset` acrescenta faces externas com winding/normais opostos, preservando UVs, tangentes, submeshes e a geometria original. Prefab sanitário e instâncias passam a usar esse resultado; material segue com culling normal. Nos femininos, conjunto extra desativado e seis pias originais de parede recuperadas/ampliadas, com espelhos alinhados a elas. Bancadas centrais removidas da circulação; não foram modificadas as cabines. Total atual: **24 pias ativas**.
- **Coordenação:** desativado somente o antigo `Blue tactile plastic` junto à placa de identificação. A faixa tátil dos pisos permanece. **Secretaria:** laptop girado 180° em relação ao estado anterior; tela e teclado ficam voltados para a cadeira do funcionário, traseira para o guichê. Conferido dos dois lados.
- **Visibilidade no apagão:** a geometria Lit conserva seus materiais para receber uma luz local fraca, enquanto emissão e placas unlit continuam apagadas. Spot junto à câmera: alcance **4,5 m**, intensidade **8,5**, cone 140°/95°, tom frio e sombra local em tier baixo; não há iluminação ambiente geral no apagão. Inimigos em sprite recebem atenuação por distância. Esse ajuste substitui o preto absoluto do primeiro teste.
- **Mão/arma da HUD:** `WeaponSpriteAnimator.WeaponImage` expõe a imagem existente ao controlador do mapa. O alarme aplica tint **(1; 0,15; 0,09)** no vermelho e **(0,17; 0,20; 0,25)** no escuro. Vida, munição e demais imagens da HUD não recebem tint. Desativar o evento restaura a cor anterior e destrói a luz local, evitando carregar o efeito para outro mapa. O prefab do player permanece sem alteração.
- Play Mode: coleta real da chave, porta da manutenção, **quatro travessias completas pelos banheiros**, mão/arma nas duas fases, visão próxima, sprite de inimigo próximo, seis chegadas, pausa e restauração de mundo/HUD passaram com **zero erros runtime**. Capturas incluem a HUD real em 1920 × 1080 e vistas 3D em 1600 × 900. Luminância média da vista 3D comparável: vermelho **0,03134**, apagão com luz próxima **0,01686**; valores são desse enquadramento inclinado para o piso e não medem todo o prédio nem desempenho. O alcance da luz preserva o fundo escuro.
- Preservados todos os **25.582 fileIDs anteriores da cena e 25.519 do prefab**, além dos GUIDs anteriores. Evidências e ferramentas locais em `.local/LawSchoolAlarm/`, prefixo `refine-`; fontes normativas `.codex/PROMPT_MESTRE.md` e `.codex/SKILL.md`. Escopo desta rodada: uma cena (`LawSchool`), seu prefab e dependências afetadas; delta corrigido e validado. Sem nova rodada de menu/portais, reentrada ou benchmark de FPS.
- Oclusão recalculada com a parede da manutenção recuperada. Build Windows Development de LawSchool + CampusExterior: **Succeeded, zero erros, sete avisos CS0618 preexistentes, 38,05 s**. Checklist local atualizado com os oito ajustes deste pedido, totalizando **63 itens preenchidos concluídos**. Prévias em `output/Direito-Ajustes-{Manutencao,Parede-Manutencao,Quadro-Eletrico,Banheiro-Feminino,Pias,Secretaria,Coordenacao,HUD-Vermelho,HUD-Apagao}.png`.

### Protótipo da chave e alarme — 06/11/2026

Esta subseção registra o primeiro protótipo, anterior à revisão de visibilidade e dos ambientes acima.

- Escopo: **uma cena, LawSchool**, e seu prefab. Delta aplicado por Unity API, preservando layout, escala, objetos e fileIDs anteriores. O prefab do player não foi alterado. Usa a manutenção 104 existente, sem criar sala duplicada.
- Chave: sprite de **0,36 m de altura** na mesa da coordenação, posição local da sala **(-0,55; 0,985; 6,70)**, com volume de coleta e billboard. `LawSchoolCellKeyPickup` registra `professor-cell-01` em `PlayerCellKeys` antes de iniciar o alarme e oculta o pickup. Uma segunda tentativa não reinicia o evento. Não altera `PlayerPickupKey.key01/02/03` nem `PrisonProgress.HasKey` e não abre portas/celas nesta etapa.
- Controlador: `Protótipo — chave de cela e alarme`, sob a raiz do prédio. Converte as **209 luzes originalmente habilitadas** em vermelho; mantém desligadas as que já estavam apagadas. No apagão, desliga as luzes e a iluminação indireta/reflexão e escurece os materiais com property blocks, incluindo emissão, placas unlit e sprites inimigos. Materiais compartilhados não são editados em runtime. Desativar o controlador restaura luzes, cores, property blocks, ambiente e áudio.
- Tempos expostos no inspector: vermelho inicial **2 s**, apagão **2 s**, vermelho repetido **3 s**, intervalo entre chegadas **1,2 s**, distância mínima de chegada **10 m**. Não usa flashes rápidos. A pausa congela o ciclo e as chegadas, além de pausar a sirene.
- Até **seis zumbis**, variante do `Global/Prefabs/Enemy/Enemy.prefab`, com movimento por Rigidbody/colisão já existente, **sem NavMesh novo**. Velocidade 2,2 m/s, cápsula de 2,12 m e escala de mundo 1, independentemente da escala 1,25 do prédio. Pontos nos corredores 2, 1, 4 e 3; espaço físico, distância e visada aberta da câmera são conferidos antes de cada chegada. Pontos inseguros aguardam uma tentativa posterior; não há leva infinita.
- Assets reaproveitados: sprite de chave da prisão movido para Global, conservando **GUID `24ba5eab46b34db2b55cbd7c6002d6a8`**, conteúdo e importador; consumidores por GUID preservados e nenhum caminho textual ativo antigo encontrado. O som é uma sirene sintetizada original de dois tons, loop mono de 2 s, PCM 16 bits/22,05 kHz, volume do AudioSource 0,22. Não houve download de assets nesta etapa.
- Persistência limitada ao protótipo: o player conserva a chave na sessão; ao retornar ao bloco, o pickup continua recolhido e o alarme reinicia com a leva da nova instância da cena. Ainda não persiste a fase exata ou os inimigos mortos em checkpoints/disco. Painel interativo, saída trancada, fuga, celas e conclusão aguardam a próxima decisão do usuário. Design em [LawSchool-Ideias-de-Mecanicas.md](LawSchool-Ideias-de-Mecanicas.md), seção final do primeiro protótipo.
- Play Mode com player/câmera reais: coleta física, inventário específico, proteção contra disparo repetido, primeira fase vermelha, apagão, retorno do vermelho, pausa de relógio/chegadas/áudio, seis chegadas, movimento da IA e restauração ao desativar passaram; **zero erros runtime**. Captura 3D do apagão, sem HUD, com luminância média **0**, primeiro apagão observado a **2,003 s**, vermelho de retorno a **4,051 s**. Seis capturas a 1600 × 900; testes/evidências locais em `.local/LawSchoolAlarm/`.
- Build Windows Development de LawSchool + CampusExterior: **Succeeded, zero erros, sete avisos CS0618 preexistentes, 45,82 s**. Preservados os **25.556 fileIDs anteriores da cena e 25.493 do prefab**. Sprite compartilhado e `.meta` conferidos contra o conteúdo original do Git; GUID/importador preservados. A mudança não altera a arquitetura estática, e a oclusão da revisão anterior foi mantida. Menu/portais, celas da prisão, reentrada e benchmark de FPS não receberam nova rodada de teste; esses resultados não são inferidos do Play Mode do protótipo.
- Capturas para revisão em `output/Direito-Prototipo-{Chave,Vermelho,Apagao,Zumbis}.png`. Ferramentas temporárias fora de `Assets` após verificar dependências, em `.local/LawSchoolAlarm/EditorTools/`. Nenhuma pasta de auditoria/backup permanente foi recriada.

### Segunda rodada do checklist — 06/11/2026

Esta rodada antecede o protótipo acima; a menção ao alarme somente como proposta descreve aquele momento da revisão.

- Paredes: união persistente de **348 volumes estruturais**, com fechamento até o forro e material PBR próprio de dupla face. Resultado em `Meshes/VideoFix/ContinuousFacultyWalls.asset`: **6.840 vértices, 3.420 triângulos**, render e MeshCollider correspondentes. Objetos antigos permanecem no asset, com render desativado quando substituído. Os BoxColliders originais foram preservados; a malha contínua fecha as junções. Não reativar o revestimento antigo sobre a nova malha.
- Sala 127: folha funcional, abertura de largura correspondente e batentes contínuos; o grande vão vazio anterior foi eliminado. `LawSchoolLighting` limita o near clip da câmera de jogo a **0,06 m** enquanto este cenário está ativo, pois o plano anterior de 0,30 m atravessava a parede antes do contato da cápsula de raio 0,237 m. O valor anterior é restaurado ao desativar o componente.
- Longarinas do hall orientadas para a circulação; mesa de formulários no acesso ao corredor 2 desativada. Estação da secretaria aproximada do guichê, com cadeira do funcionário voltada para o atendimento.
- “Sala do diretor”, nesta solicitação, significa **Coordenação do Bloco de Direito**. Mesa, cadeira de trabalho e duas poltronas de atendimento reposicionadas; laptop, luminária e prancheta apoiados no tampo. Mobiliário antigo que deixava objetos flutuando continua desativado, preservando apenas os livros de arquivo aproveitados.
- Copa preservada e ampliada para **seis cadeiras**. Placas de sala com **0,65 × 0,213 m**, de serviços com **1,175 × 0,30 m** e suspensas com **1,688 × 0,45 m**, em mundo; textos rasterizados no material e suportes compatíveis. Lousas sem o estiramento horizontal anterior.
- Sanitários: cubas ampliadas em 1,35×, bancadas com recortes reais e melhor ocupação do feminino; **32 cubas ativas** no total. Espelhos afastados da parede/revestimento para aparecerem inteiros, usando `Washroom non-reflective aged glass`. **Os oito componentes `LawSchoolMirror` estão desabilitados**, sem câmera de reflexão ativa; script e shader foram preservados por serem assets serializados. Este pedido substitui o de reflexão da primeira rodada.
- A antiga aula 104 agora é **`MANUTENÇÃO 104 — painel elétrico e ferramentas`**, com quadro, bancada, ferramentas em escala de mão, arquivo e estante. O painel é cenário estático nesta revisão. O alarme, bloqueio da saída e interação de energia permanecem somente como proposta em [LawSchool-Ideias-de-Mecanicas.md](LawSchool-Ideias-de-Mecanicas.md), na seção final “Direção proposta pelo usuário — a chave dispara o alarme”. Cada chave proposta continua abrindo uma única cela de professor.
- Identidade: preservados os **23.234 fileIDs da cena e 23.171 do prefab** existentes no início desta rodada; nenhum fileID anterior perdido e prefab do player sem alteração.
- Validação: **636 amostras de passagem**, 36 aulas e manutenção 104, 40 portas de ambientes, quatro ligações sanitárias, guichê livre, longarinas voltadas para a circulação, seis cadeiras na copa e zero TMP estático habilitado. Piso mantém **77 tags `Eco` e duas `Wood`**.
- Play Mode: porta de funcionários da secretaria e nova porta da sala 127 abriram por proximidade e foram atravessadas pelo player real; quatro percursos completos nos banheiros com `CharacterController.Move`; **14 vistas e uma captura em contato com a parede**, câmera a Y=1,898 m, zero erros. Distância câmera/parede **0,287 m**, near clip efetivo **0,03 m**, sem revelar o vazio.
- Oclusão recalculada após a geometria final. Build Windows Development de LawSchool + CampusExterior: **Succeeded, zero erros, sete avisos CS0618 preexistentes**, **113,68 s**. Não foram executados benchmark de FPS nem nova rodada de menu/portais. Os avisos estão nos quatro scripts identificados no histórico da primeira rodada.
- Checklist local: **55 itens preenchidos marcados `[X]`**; espaços vazios preservados para novas anotações. Capturas desta rodada em `output/Direito-Revisao2-{Sala127,Placas,Copa,Banheiro,Coordenacao,Secretaria,Manutencao}.png`. Ferramentas temporárias verificadas e mantidas fora de `Assets`, em `.local/LawSchoolVideoFix/EditorTools/`; não são dependências do jogo.

### Primeira rodada do checklist — histórico de 06/11/2026

Os valores desta subseção descrevem a primeira rodada. Para layout, manutenção, espelhos e sinalização atuais, prevalecem a Leitura rápida e a segunda rodada acima.

- Fonte de tarefas: `.codex/reference/problemas-video-direito.md`, mantida local e ignorada pelo Git, incluindo os novos itens gerais adicionados durante a revisão. As alterações foram aplicadas incrementalmente por Unity API à cena e ao prefab, preservando os objetos e fileIDs preexistentes.
- Secretaria: sala fechada junto ao hall/corredor 4, com a porta original para funcionários; abertura de atendimento no lado do hall, balcão, estação com laptop, arquivo e copiadora. Paredes e colisores receberam abertura recortada; a recepção provisória dentro do hall foi desativada. A pilastra na frente do guichê foi retirada.
- Dois banheiros lado a lado em cada extremidade lateral, compartilhados pelos corredores **2/1** a oeste e **4/3** a leste. Cada banheiro conecta os corredores por duas entradas, mantendo circulação central separada das cabines. Substituem as antigas salas de serviços e as faixas de salas nas extremidades, incluindo a antiga sala 108 de manutenção. Outros sanitários/ambientes duplicados foram convertidos em salas de aula. Há colisão em vasos, cubas, mictórios, divisórias e folhas abertas das cabines; os blocos cinza antigos ficaram desativados.
- Coordenação única: retirados telefone e segunda mesa de reunião com quatro poltronas; atendimento, trabalho e arquivo possuem zonas distintas. A antiga sala de apoio foi substituída por uma copa no meio do corredor 1, com armários, bancada, cuba, geladeira, micro-ondas e mesa. O micro-ondas existente de Adam Nekola/Poly Haven foi compartilhado em `Global/{Models,Textures,Materials}/Equipment/vintage_microwave/`, preservando GUIDs.
- Todas as 37 salas possuem lousa larga. Avisos de manutenção, placas docentes, quadros de plano de ensino, remendos volumétricos e estantes das salas de aula foram desativados. As lousas usam seis inscrições de giz no próprio atlas de textura: matérias jurídicas, pedidos de socorro e mensagens de horror. As placas de corredor apresentam as faixas numéricas nas duas faces; as identificações das portas foram centralizadas para não atravessar as folhas.
- Piso de granilite interno com escala métrica de UV nas superfícies novas. Faixa tátil com relevo e áreas de atenção nas interseções; longarinas substituem as cadeiras escolares ainda ativas nos bancos antigos. O hall recebeu espera, plantas, lixeiras, avisos acadêmicos e mesa de formulários, preservando o caminho central.
- Espelhos: `Scripts/LawSchoolMirror.cs` e `Shaders/LawSchoolMirror.shader` são dependências runtime necessárias, com reflexão planar do ambiente. Só atualizam superfícies próximas e dentro do campo de visão; render de 512 px, limite de 15 atualizações/s por câmera e distância de 16 m, sem pós-processamento ou sombras na câmera refletida. O custo de FPS no executável ainda deve ser medido; esses limites não são uma alegação de ganho de desempenho.
- Texturas novas e prompts completos: `Licenses/AssetSources/LawSchool/video-revision-sources.json`; geração pelo modo **built-in imagegen**. Artefatos persistentes em `Textures/VideoFix/`, `Meshes/VideoFix/`, `Materials/VideoFix/` e nas pastas de sinalização já existentes.
- Validação desta revisão: **636 amostras de passagem**, cápsula real de **2,121 m × 0,473 m**, 37 salas, quatro sanitários com duas entradas cada, oito espelhos e nenhum TMP estático habilitado. Conferidos também os limites das novas paredes e a visada livre pelo guichê. Tags de piso ativas: **77 `Eco` e dois `Wood`**. Carteiras: **0,950 m**, cadeiras escolares/longarinas: **1,100 m**, cadeiras de escritório: **1,200 m**, estantes: **2,250 m**; player e raiz 1,25 preservados.
- Play Mode: player e câmera reais, abertura por proximidade e passagem pela porta de funcionários da secretaria, quatro travessias completas usando `CharacterController.Move` pelos banheiros entre corredores e **16 capturas** na altura dos olhos de **1,898 m**; zero erros de execução, inclusive após o recálculo de oclusão. O teste automático fecha o menu e mantém a Game View ativa, evitando confundir uma partida parada no primeiro frame com falha da porta. A cápsula técnica do prefab do player não aparece nos espelhos.
- Oclusão nativa recalculada. Build Windows Development de LawSchool + CampusExterior: **Succeeded, zero erros, sete avisos CS0618 de APIs obsoletas preexistentes**, em `GameManager`, `BasementArenaEncounter`, `PlayerRespawn` e `TCRegionAtmosphere`; esses scripts não foram alterados. Último build incremental: **18,74 s**. Não houve benchmark de FPS no executável nem repetição dos testes de menu/portais.
- Preservados todos os **19.410 fileIDs anteriores da cena e 19.364 do prefab**, além dos GUIDs do conteúdo compartilhado. Ferramentas e evidências locais desta tarefa em `.local/LawSchoolVideoFix/`, ignoradas pelo Git. O MD de tarefas conserva os espaços em branco do usuário para futuras anotações; os 42 itens preenchidos estão concluídos, incluindo os três já marcados antes desta revisão.
- Capturas para revisão em `output/Direito-Checklist-{Secretaria,Hall,Lousa,Banheiro,Espelho}.png`. Escopo desta revisão: uma cena elegível (`LawSchool`) e seu prefab; uma cena auditada, corrigida e validada funcionalmente, zero cenas bloqueadas. Fonte normativa: `.codex/PROMPT_MESTRE.md` e `.codex/SKILL.md`, seções de escala, materiais, iluminação e legibilidade. Pendência técnica fora do checklist: benchmark de FPS no executável; não foi criada auditoria permanente adicional.

### Segunda seleção — equipamentos e apoio — 06/11/2026

- Novos modelos: copiadora e projetor digital da Innerscene; bebedouro Grab3D; tela móvel de PeterM, prancheta de ProgrammerOnCoffee e grampeador de Mateusz Sadek (Poly Haven). Reutilizados do inventário: lixeira de GurJas Studios, extintor de UMJOORIN e telefone antigo de Adrian C (Poly Haven). Todos CC0, com URLs, hashes e conversões em `Licenses/AssetSources/LawSchool/equipment-sources.json`.
- **42 instâncias em 22 conjuntos**: quatro copiadoras, três projetores, dois bebedouros, uma tela, oito pranchetas, oito grampeadores, oito lixeiras, quatro extintores e quatro telefones. Oito blocos repetidos sobre mesas foram desativados. Extintores com base apoiada no chão; telefones presos à parede; tela e projetor da sala 107 preservam sua disposição lateral. Player e raiz 1,25 mantidos.
- Assets exclusivos em `Models/Equipment/`, `Textures/Equipment/`, `Materials/Equipment/`, `Meshes/Equipment/` e `Prefabs/Equipment/` sob a pasta do mapa. Os três recursos reaproveitados e suas dependências passaram a `Global/{Models,Meshes,Textures,Materials,Prefabs}/Equipment/<id>/` conforme o tipo, por `AssetDatabase.MoveAsset`, sem alterar GUIDs/conteúdo. Dependências por GUID de EntranceHallway e CampusExterior equivalentes antes/depois. Materiais originais PBR preservados; vidro do telefone adaptado à URP. Grampos soltos de demonstração foram ocultados, e o bebedouro recebeu cores mais discretas.
- Cena/prefab conservam todos os **19.282/19.236 fileIDs anteriores**. **321 amostras** de passagem com a cápsula real, **19 apoios no piso**, **19 apoios em tampos** e verificação de overlaps passaram. Conservadas 180 superfícies de texto físico, zero TMP estático habilitado e tags de 81 pisos `Eco`/dois `Wood`.
- Play Mode: seis vistas a 1600 × 900, pés reais da cápsula em Y=0,060 m, câmera em Y=1,898 m, zero erros de execução. Build Windows Development de LawSchool + CampusExterior: **Succeeded, zero erros, zero avisos, 17,27 s** na verificação incremental final. Oclusão refeita após a última mudança. Não houve benchmark novo do executável nem repetição dos testes de menu/portais. Capturas e resultados locais em `.local/LawSchoolEquipment/`, ignorados pelo Git; ferramentas temporárias não são dependência runtime.
- Problemas anteriores anotados no vídeo (placas, patches, piso, espelhos e iluminação) não são considerados resolvidos por esta troca de equipamentos.

SHA256 da segunda seleção:

| Arquivo sob o prefixo do mapa | SHA256 |
|---|---|
| `LawSchool.unity` | `17c8af8b630fa0cef177e0a3dc9de4f8fc669a0be720dbe70be0bf2949b7d463` |
| `Prefabs/LawSchool.prefab` | `517e596e8be631d57f9fa7d554af2f0c0e389aa55294c860cb7cbb739eeda6bd` |
| `LawSchool/OcclusionCullingData.asset` | `155362fb7ea170fb08658939eff3eb0607c92d7c5af4dd80bd9cbd263e6cc851` |

### Substituição por modelos autorais CC0 — 06/11/2026

- Modelos novos: laptop antigo de Arrangemonk, blocos de escritório de Ulan Cabanilla e estante metálica de James Ray Cock (Poly Haven), cadeira giratória de nisu/3DModelsCC0 (OpenGameArt) e longarina de quatro lugares da Innerscene. Modelos existentes reaproveitados: mesa metálica, poltrona de atendimento, luminária articulada, estante de madeira e arquivo de gavetas da Poly Haven. Autoria, URLs, hashes e conversões estão no manifesto JSON de proveniência.
- Pastas exclusivas: `Models/Furniture/`, `Textures/Furniture/`, `Materials/Furniture/`, `Meshes/ImportedFurniture/` e `Prefabs/Furniture/` sob a pasta do mapa. Fontes já usadas em outros cenários foram compartilhadas em `Global/Models/Furniture/`, `Global/Materials/Furniture/` e `Global/Textures/Furniture/`, mantendo seus GUIDs. Estantes/arquivos já compartilhados continuam em `Global/Models/Library/`.
- 43 mesas metálicas (31 docentes e 12 administrativas/reunião), 30 poltronas, dez cadeiras giratórias, quatro longarinas, 16 estantes de madeira, 12 arquivos, quatro estantes metálicas, dez laptops, oito luminárias e 57 conjuntos de blocos/documentos. A sala 105 conserva a composição de audiência simulada. Carteiras/cadeiras escolares e sanitários existentes continuam em uso.
- Alturas em mundo: mesas **0,95 m**, cadeiras giratórias **1,20 m**, poltronas **1,125 m**, longarinas **1,10 m** e arquivos/estantes **2,25 m**. A escala 1,25 do edifício e o prefab do player permanecem preservados. Normalização uniforme por modelo, com pivô apoiado no chão ou tampo.
- Geometria antiga combinava móveis, revestimento, luminárias e avisos no mesmo mesh. As variantes `Meshes/ImportedFurniture/Retained_*` conservam as partes arquitetônicas, documentos e equipamentos ainda usados. Renderers/colisores dos móveis substituídos foram desativados ou passaram a usar essas variantes; objetos e assets anteriores permanecem recuperáveis. As inscrições continuam em texturas e os pontos de luz existentes foram mantidos.
- A tela do laptop usa material próprio de monitor desligado; a textura do teclado/carcaça não é aplicada ao display. PBR em URP/Lit, normais importadas como NormalMap e máscaras lineares. O GLB da longarina foi convertido para meshes nativos com correção de eixos, winding, UVs e materiais, sem dependência de importador em runtime.
- A pesquisa completa foi encerrada antes das substituições. O Markdown de pesquisa é local e consta no `.gitignore`; os assets de jogo e seu manifesto de licença devem acompanhar o projeto.

#### Validação dos modelos

- Cena e prefab: 43 conjuntos/194 instâncias, materiais presentes e shaders suportados, sem componentes ausentes; 180 superfícies de texto físico e nenhum TMP estático habilitado. Preservados **18.808 fileIDs existentes da cena e 18.762 do prefab**, além dos GUIDs e do conteúdo dos recursos compartilhados movidos. As dependências por GUID de DirectorRoom/Library permaneceram equivalentes durante os movimentos.
- **321 amostras de passagem** com cápsula de 2,121 m × 0,474 m e **119 apoios no chão**, sem falhas. Alturas e escala conferidas. Documentos ajustados às prateleiras reais; inscrições e pontos de luz mantidos. Tags de piso preservadas: 81 `Eco` e dois `Wood`.
- Play Mode com player e câmera reais: três vistas a 1600 × 900, câmera em Y=1,898 m e pés em 0,060 m; zero erros de execução. Treze capturas adicionais de escritórios, corredores e sala 101. Evidências locais: `Temp/LawSchoolModels/` (temporárias, sem dependência runtime).
- Oclusão nativa refeita depois dos ajustes. Build Windows Development **LawSchool + CampusExterior**: **Succeeded, zero erros, zero avisos, 51,31 s**. Não foram repetidos testes de menu/portais nem executado benchmark no player compilado nesta rodada; medições antigas de frame time abaixo são históricas. Problemas anteriores de iluminação, placas, piso e patches registrados no vídeo permanecem fora desta substituição de mobiliário.

SHA256 após esta substituição:

| Arquivo sob o prefixo do mapa | SHA256 |
|---|---|
| `LawSchool.unity` | `62b3a16505983a24041f39ff575346d5471fc9082b483e28b5d7baa3e3855d82` |
| `Prefabs/LawSchool.prefab` | `2bb615fce11f7050ab72e3dac01547b3db8de49cd13600700502d537028ec660` |
| `LawSchool/OcclusionCullingData.asset` | `9093c7e2bbdbbe24e07c598161828f0d3f01e0ab26ff50aff5a1985c89135627` |

### Fechamento e proporções anteriores

- A inspeção diferenciou parede sem render de **junta sem geometria nem colisor**: 8.898 amostras nas paredes originais com colisores passaram, mas 77 amostras nas fronteiras do hall e fachadas internas mostraram o vazio. Foram adicionados fechamentos persistentes nas juntas entre salas, ligação com o hall, encontro central das alas e perímetro do piso/forro do hall. As folhas, ferragens e painéis da entrada foram completados; a placa de saída substitui a identificação sobreposta.
- Coordenações: mesa ampla, computador/periféricos, cadeira de trabalho, assentos de atendimento, mesa de reunião, estantes de processos, arquivo e avisos acadêmicos. Secretarias: atendimento, duas estações de trabalho, espera e protocolo. Apoio/serviços: estação de trabalho, copiadora e arquivo de equipamentos. As oito salas administrativas mantêm portas e funções originais.
- As 32 salas de aula receberam mesa docente adequada e quadro de informações acadêmicas, sem perder seus arranjos de carteiras, seminário e audiência. Novos elementos usam materiais/texturas existentes; não houve download de modelos ou texturas adicionais.
- `Meshes/Repair_*` e os quatro novos materiais na pasta do mapa são resultados persistentes, referenciados pela cena/prefab. Não há geração em runtime. A raiz de escala também amplia assentos, sanitários, portas, paredes e sinalização antigos. A altura das chegadas foi preservada e o alcance/intensidade das luzes existentes foi compensado; os escritórios têm luminárias e abajures visíveis como fontes das novas luzes.
- Normas: [skill](../../.codex/SKILL.md), [prompt mestre](../../.codex/PROMPT_MESTRE.md) e referência `direito reference.png`. Comparação visual com as capturas oficiais Hotel Lobby e Shader implementation do mod clássico; sem copiar assets.

### Textos físicos, disparo e passos

- **181 componentes de texto estático foram tratados**: 180 superfícies receberam PNGs e materiais URP/Lit em `Textures/Signage/`, `Materials/Signage/` e `Meshes/Signage/`, e um texto sem geometria visível foi desativado. Os 32 conteúdos de lousa usam escrita manuscrita com desgaste de giz, incorporada à face inteira da lousa; placas, avisos e identificação usam impressão sobre a superfície. Nenhum componente TextMeshPro do prédio permanece habilitado. Componentes e strings originais foram preservados desabilitados, com seus fileIDs; HUD e menus continuam dinâmicos.
- A fonte manuscrita foi usada somente na rasterização local; o arquivo de fonte do Windows não foi incluído. Não há dependência runtime da fonte de rasterização nem da ferramenta temporária. Para mudar uma inscrição, altere sua imagem/material e mantenha o texto-fonte coerente no componente desabilitado; não reative texto flutuante sobre a lousa.
- O disparo falhava com `NullReferenceException` em `Pistol.Shoot`, ao tentar acessar `SFXManager.Instance` em uma cena aberta diretamente. `Global/Scripts/Core/SFXManager.cs` agora cria o serviço configurado em `Global/Resources/Audio/SFXManager.prefab` quando a primeira cena não contém um; gerentes já existentes são preservados. Pistola e shotgun também protegem a chamada de áudio. O disparo deixa de depender de uma cena inicial específica.
- **83 colisores de chão/palco** receberam tags consumidas pelo `FootstepController`: `Eco` para circulação/pisos institucionais e `Wood` para palco e degrau de madeira. Tags existentes foram reutilizadas; o player e os bancos de sons de passos foram preservados.

### Validação anterior — fechamento, textos e áudio

- Cena e prefab salvos e reabertos pela Unity. Todos os fileIDs originais foram preservados; novos resultados usam assets persistentes. A cápsula e o prefab do player não foram redimensionados.
- Faces estruturais: **10.542 amostras** sem ausência de geometria; fechamento do hall/fachadas: **2.936 amostras**, zero frestas detectadas após o reparo, contra 77 amostras com vazio antes. Verificação física: **321 amostras**, incluindo os 49 acessos e o eixo central.
- Play Mode com o player real: **49 portas atravessadas**, chegada ao Campus Exterior e retorno a `FromCampus` em `(0; 1,12; 5,25)`. A primeira automação atravessou uma folha ainda em animação na sala 132; o teste foi corrigido para aguardar a abertura completa e o percurso integral passou, sem alterar a porta por uma falha de sincronização do teste.
- Disparo real: uma munição consumida, evento `OnShoot` emitido e serviço de SFX configurado presente. Caminhada: **351 eventos de passos**, com clipes `WalkEco03` a `WalkEco08` reproduzidos durante o percurso.
- Oclusão nativa recalculada depois da alteração de escala e da inclusão das superfícies texturizadas. Capturas Unity em **1600×900**, incluindo vistas pela câmera real do player, com olhos **1,839 m acima dos pés**.
- Capturas finais após integrar a escrita à face inteira das lousas: `output/Direito-Revisao-{Hall,Saida,Sala101,CoordenacaoAtendimento,Banheiro,Auditorio}.png`. São renders Unity com FOV 60° na altura do jogador; as capturas intermediárias da câmera real e logs do percurso ficam no temporário desta revisão.
- Checklist interno Total Chaos: **20/24** — escala 2; materiais 1; desgaste 2; fontes de luz 2; sombras/navegação 1; profundidade 2; uso/abandono 2; props 2; legibilidade de detalhes 1; identidade 2; pós-processamento 2; desempenho 1. Materiais/sombras, leitura à distância e desempenho são avaliações parciais; não substituem revisão visual do usuário nem benchmark atualizado.
- **Build Windows Development de LawSchool + CampusExterior concluído: zero erros, sete avisos de APIs obsoletas em código preexistente de GameManager, PlayerRespawn, BasementArenaEncounter e TCRegionAtmosphere.** Duração aproximada: 126 s. Executável de verificação em `Temp/LawSchoolRepair/Build/UniDoom.exe`; não é um build completo das dez cenas.
- Ferramentas desta revisão retiradas de Assets após verificar dependências de todos os assets do projeto e o build. Os códigos temporários ficaram em `Temp/LawSchoolRepair/EditorTools/`; o jogo depende apenas dos resultados persistentes e do serviço de áudio global. Foi descartada somente uma amostra de textura/material/mesh criada nesta revisão e sem consumidores. Relatórios atuais em `Temp/LawSchoolRepair/review/`; documentação/capturas duráveis fora de Temp. A compilação do Editor também passou após a retirada das ferramentas.
- Não foram repetidos benchmarks de GPU/FPS ou testes em outras máquinas. As métricas antigas abaixo não se aplicam automaticamente à geometria atual.

Hashes finais da revisão de 06/11/2026:

| Arquivo sob o prefixo do mapa | SHA256 |
|---|---|
| `LawSchool.unity` | `10f4f6cc921989715212c9dbbcd60656147d555fdbae92bd98e4511078022e4b` |
| `Prefabs/LawSchool.prefab` | `bdba8bcd76c21ce0477ba0f4ebc94ad03b0ebf5a60cda5146af9fd8d320ccb33` |
| `LawSchool/OcclusionCullingData.asset` | `bfcf80adf3db32015d7d2c8ba292e5b7b18c736190b43bb3d17476780c4f2c7b` |

O refresh do Editor também atualizou o cache do font fallback compartilhado e os campos `userData` das duas silhuetas de inimigos importadas pelo merge. Essas alterações automáticas foram preservadas; não houve mudança intencional de aparência/comportamento dos inimigos nesta revisão.

## Validação anterior — histórico, não representa a revisão atual

Em 2026-11-05: build Windows Development de **LawSchool + CampusExterior**, zero erros; teste real do menu, 32 entradas de salas, oito banheiros, corredores internos, portais nos dois sentidos e passagem pela porta/eixo central/palco do auditório. Auditoria estática: **2.406 amostras**, sem falta de piso nem obstruções para a cápsula real. Oclusão recalculada e persistente.

RX 6400, D3D12, janela 1600×900, cinco vistas fixas, mediana de 180 amostras após aquecimento:

| Vista | Frame ms | GPU ms |
|---|---:|---:|
| Hall | 4,612 | 4,101 |
| Corredor | 6,163 | 5,553 |
| Sala | 5,543 | 5,003 |
| Banheiro | 4,289 | 3,851 |
| Auditório | 4,313 | 3,897 |

Valores absolutos de uma máquina; não são promessa de FPS geral nem comparação equivalente com a planta pequena. Não testados: build das nove cenas, automação de Play Mode do Editor, tela cheia, outras GPUs, VRAM/GC/overdraw e certificação de acessibilidade.

Identificação SHA256 da versão validada (compare sem ler o YAML inteiro):

| Arquivo sob o prefixo do mapa | SHA256 |
|---|---|
| `LawSchool.unity` | `3844708f84b3c4a30ed561f5782a1972108c18673d3be25da0db0e7dbc931262` |
| `Prefabs/LawSchool.prefab` | `2bbf6c7c9f6cce29f6b5cb2ff274a758ae622e208a8dd4968dc3ef4c6d6a9eb8` |
| `LawSchool/OcclusionCullingData.asset` | `b23c5ab5b5c5ea00d3a13c166c1d1fe79846f65e94274afc9ebdafe3e716c66f` |

Esses hashes identificam somente os três arquivos; materiais, importadores, código e demais dependências também podem invalidar um teste anterior.

Capturas duráveis: `output/Direito-Hall.png`, `Direito-Salas-Ampliadas.png`, `Direito-Banheiros-Ampliados.png`, `Direito-Auditorio.png`. Evidências detalhadas, quando ainda presentes: `%TEMP%/UniDoomLawExpansion/{validation.txt,build.txt,occlusion.txt,cleanup.txt,runtime/,final-runtime/,review/}`. O mapa **não depende** desses temporários. Ferramentas desta ampliação foram removidas após verificar dependências; as oito ferramentas Editor preexistentes foram preservadas.

## Manutenção sem repetir investigação

1. Leia o pedido novo e esta ficha; veja a referência e uma ou duas capturas relevantes. Localize os objetos pelo Editor, sem despejar o YAML inteiro da cena/prefab.
2. Inspecione estado real e alterações existentes. Não rode geração sobre Play Mode. Reconcilie alterações não salvas antes de reabrir uma cena.
3. Ajuste somente a categoria afetada. Salve resultados persistentes por Unity API. Refaça oclusão se mudar paredes/vãos/occluders; mudanças apenas em docs não pedem novo build.
4. Valide o delta: escala/colisões → cápsula e percurso; iluminação → capturas/frame time; portais/menu → transições/build list. Expanda testes se houver falha ou dependência alterada.
5. Atualize esta ficha, os manifestos de fontes e o que foi efetivamente testado. Não reutilize automaticamente estes resultados se cena ou dependências mudaram.

Armadilhas observadas:

- `Renderer.bounds` de um FBX carregado só como asset pode retornar zero. Use `Mesh.bounds`/vértices transformados ou uma instância de preview para normalizar dimensões.
- Copiar um `Mesh` com `CopySerialized` pode deixar buffers/limites desatualizados. Se ocorrer, reaplique dados via `Mesh.Clear/SetVertices/SetTriangles`, recalcule bounds/normais/tangentes conforme necessário, reassocie o mesh e confirme salvo/reaberto e renderizado. Preserve a topologia; não presuma organização de vértices sem inspecionar.
- `VolumeProfile` criado por código precisa dos componentes adicionados ao asset para persistirem. Referências runtime necessárias devem existir antes de retirar o gerador.
- Use `EditorSceneManager.NewPreviewScene()` para auditoria isolada; não confunda vãos abertos com portas faltantes. Depois da cópia, consulte transforms de entrada, não exija componente de porta em toda sala.
- Editor aberto no mesmo projeto: não inicie outro Editor em batch. Um `MenuItem` temporário explícito é mais confiável que depender somente de polling em Play Mode. No Windows, o menu nativo `Assets/Refresh` pode ser acionado por `GetMenu/GetSubMenu/GetMenuItemID` e `WM_COMMAND`, sem enviar Ctrl+R para outro aplicativo. Resolva IDs dinamicamente; nunca fixe PID/handle.
- Teste de FPS exige janela realmente renderizando, triângulos não zero e aquecimento. Janela oculta/minimizada não fornece benchmark comparável.
- Confira espaço de disco. O primeiro build em `%TEMP%` falhou por C: cheio; o build válido usou `Temp/LawSchoolExpansionBuild` no disco do projeto. Temporários do projeto podem desaparecer ao fechar a Unity; mantenha documentação permanente fora deles.

## Solicitações originais — consulta opcional

Trechos integrais dos três pedidos que definem este mapa, preservando a redação do usuário. O resumo operacional está no início; não é necessário reler este apêndice em toda edição.

Criação:

> Seguinte, tem dentro da pasta de refenrece , um png chamado direito reference, usa ele de referência, e cria esse mapa, já faz ele ligado ao prédio no campus entrance da unimar, e já adiciona ele na tela do menu principal naquele menu temporário, usa as skills e o prompt mestre passado, faça o mapa ser otmizado, utilize modelos 3d gratuitos sempre que possível

Variação das salas:

> tenta faze as salas um pouco diferente umas das outras, tipo, uma sala de aula ter menos cadeiras que a outra, a lousa tá do lado oposto da porta, etc, faz cada ter uma peculiaridade

Ampliação e escala, pedido mais recente:

> Faça o mapa bem maior, há salas muito pequenas comm1 carteira, as salsa devem parecer igual as de faculdade, o mesmo vale para os banheiros, faça o mapa levando em consideração o tamanho do player, não se preocupe do mapa ficar grande, o bloco precisa parecer um bloco de direito de uma faculdade, o espaço precisa ter um estilo mais assustador se possível semelhante a total chaos, reanalise o mapa, faça as alterações necessárias

## Manutenção trancada e horda reduzida — 08/11/2026

Aplicável ao **bloco de Direito**, conforme correção do usuário no pedido inicialmente escrito como Bloco 4. Coletar a chave sorteada da cela 1 continua disparando o alarme imediatamente, sem depender das chaves 2/3. `LawSchoolMaintenanceAccess` acompanha o evento existente e revela um único `LawSchoolMaintenanceKeyPickup`, inicialmente inativo. Usa as 33 posições já elegíveis da chave do alarme, excluindo a sala em que ela apareceu: **32 escolhas atuais**, fora do corredor 2/manutenção. Sorteio, não repetição entre partidas e posse reutilizam `PlayerCellKeys`; não há inventário ou save paralelo. O local permanece ao retornar na mesma sessão; a chave coletada não reaparece.

A porta da manutenção 104 mantém folha/animação/colisão e exige `OpenDoorWithKey.requiredInventoryKey = law-school-maintenance`. Campos vazios preservam o comportamento das demais portas. Chaves das celas e chaves 01–03 antigas não substituem a da manutenção. Quadro, sirene, fases de luz, persistência do alarme e bloqueio da saída preservados. O pool de chegadas passa de **60 para 48**: removida uma referência a cada cinco, sem apagar marcadores ou mudar regras de segurança/variantes. Os zumbis já presentes permanecem após desligar o alarme.

Validação em Edit Mode: configuração salva/reaberta, 32 locais após exclusão da sala da chave 1, pickup inicial oculto, limite de 48 e condição da porta em inventários isolados. Previews `output/Ultimos-Itens-{Chave-Manutencao-Direito,Porta-Manutencao-104}.png`. Cenas, importadores e IDs anteriores preservados. **Sem jogo/Play Mode, coleta/alarme/horda/animação/retorno entre cenas em runtime, abrir/focar janela, build ou FPS.** Registros antigos de Play Mode acima não validam este novo requisito de chave.

## Chaves disponíveis desde a entrada — 09/11/2026

As duas chaves do fluxo alarme/manutenção são posicionadas na entrada: a chave da cela 1 usa o sorteio existente e a chave da manutenção usa outra sala elegível, excluindo a origem da primeira e mantendo a exclusão da ala de manutenção. A manutenção começa trancada; não é necessário iniciar o alarme para sua chave aparecer. Posse e escolha continuam no inventário persistente existente.

O alarme só retoma quando a coleta local da chave de Direito tiver registrado law-school-local-key-collected. Ter uma chave obtida no Bloco 4 não inicia esse evento. Horda máxima de 48 e desligamento no quadro preservados. Implementação salva, sem executar testes, jogo ou Play Mode. Este contrato substitui o registro anterior de aparecimento da chave apenas após disparar o alarme.

## Contrato atual das duas chaves — 09/11/2026

Exatamente duas chaves ativas em Direito: cela 1, que inicia o alarme, e manutenção, que abre exclusivamente a manutenção. Ambas disponíveis desde a entrada em salas diferentes, fora da ala de manutenção e separadas por pelo menos 45 m. A escolha persistida é mantida apenas se ainda satisfizer essa distância. Há 15–28 opções válidas para cada uma das 33 salas de origem.

A entrada da manutenção possui colisor sólido salvo e ativo cobrindo todo o vão, separado da folha móvel. `LawSchoolMaintenanceAccess.entranceBarrier` só libera quando existe `law-school-maintenance` no inventário. `OpenDoorWithKey` também exige essa chave. O quadro exige as duas chaves. Originais das celas 2/3 em Direito inativos; pickups transferidos para a biblioteca para preservar a progressão das oito celas.

Horda: máximo 38 chegadas. Lanterna automática durante o alarme, F/direcional para cima alterna; feixe de 18 m/58°. Sem abrir Unity ou executar gameplay, testes, compilação, build ou capturas nesta rodada.

### Correção aplicada na cena carregada pelo jogo

A investigação da reincidência confirmou que a cena não referencia o prefab principal. Na cena, a porta real `OpenDoorWithKey` de fileID `908449428` não tinha `requiredInventoryKey`; o pickup da manutenção e seu controlador também estavam ausentes. As correções anteriores no prefab não chegavam ao mapa jogável.

Agora `LawSchool.unity` contém a exigência `law-school-maintenance` nessa porta, o colisor sólido ativo cobrindo o vão, o pickup e `LawSchoolMaintenanceAccess`. O controlador está registrado no objeto do alarme da própria cena e usa seus 33 locais de sorteio fora do corredor da manutenção, com distância mínima de 45 m da chave da cela. Há exatamente os dois pickups: cela 1 e manutenção. As chegadas do alarme na cena também foram alinhadas ao limite de 38 já solicitado.

Todos os IDs anteriores da cena foram preservados; 12 registros adicionados e referências locais conferidas. Sem abrir Unity, testes de gameplay, capturas, commit, push ou desligamento nesta correção.

## Redistribuição e portas após o merge — 09/11/2026

O pedido de equilíbrio substitui a limitação anterior de duas chaves em Direito: há quatro de cela (1/2/3/9) e uma de manutenção. Biblioteca recebe cinco e Bloco 4 quatro chaves. Três pickups foram acrescentados na cena jogável e no prefab independente; locais reservados às salas 110/121/132 não participam dos sorteios da cela 1/manutenção.

O merge alterou a abertura das portas comuns para contato, mas `RequiresAnyKey` considerava apenas os três booleans antigos. Corrigido para incluir `requiredInventoryKey`: a manutenção não pode abrir por contato sem sua chave e continua liberando por aproximação quando ela está no inventário. O sentido de abertura por contato também consulta `openingSideAxis` via `PlayerSide`, preservando as catracas.

Mudanças locais, sem commit, push, Unity ou Play Mode.

## Sprite da manutenção e redução da horda

Pedido corrigido pelo usuário para **Direito**, não Bloco 4. Sprite novo em `Textures/Pickups/MaintenanceKey.png`, gerado pela ferramenta imagegen, sem reutilizar a identificação das chaves de cela. Metal gasto, plaqueta amarela com símbolo de chave de boca, sem letras ou números. PNG original preservado com alpha; importer Single Sprite, Point, sem compressão e 1024 PPU. Escala ajustada pelos limites visíveis do alpha para aproximadamente 24 cm de altura, mantendo posição e collider do pickup. Origem e hash em `Licenses/AssetSources/LawSchool/maintenance-key-sprite.json`.

O limite de reforços do alarme passou de **38 para 34** nas duas cópias do mapa: `LawSchool.unity` e `Prefabs/LawSchool.prefab`. São quatro referências a menos, distribuídas pela lista. Redução real de 10,53%, por arredondamento para número inteiro; marcadores, variantes e regras de spawn seguro preservados. Sem apagar objetos de spawn.

Conferência por PNG/importador/referências e arrays serializados. Sem abrir Unity, jogo, Play Mode, build, capturas, commit ou push.

## Primeiro pickup de qualquer chave e alarme único — 09/11/2026

`LawSchoolAlarmPrototype.NotifyKeyCollected` centraliza o disparo dos três tipos de pickup: chave original da cela 1, chaves compartilhadas 2/3/9 e chave da manutenção. Somente uma coleta nova no mesmo cenário do controlador pode iniciar o evento. A primeira registra `TriggeredEventId` no inventário persistente; coletas seguintes não iniciam outra sirene nem outra leva. `EventId` registra o desligamento e impede novo disparo também ao retornar ao bloco. Biblioteca e Bloco 4 não acionam o alarme de Direito.

`TryResolve` exige a chave da manutenção, vida e interação válidas com o alarme ativo; deixou de exigir a chave 1. Assim é possível desligar o quadro quando a manutenção for a primeira chave coletada. A porta da manutenção continua trancada pela chave específica. Sorteios, separação, distribuição e as 34 chegadas existentes preservados.

Validação sem Editor: compilação C# isolada da lista real de fontes/referências de `Assembly-CSharp`, sem erros; apenas avisos preexistentes. Harness com os métodos reais extraídos e dublês de cena/apresentação passou nas 120 ordens das cinco chaves, incluindo 60 desligamentos antes de obter a chave 1, bloqueio sem manutenção, duplicatas, persistência e pickups em outros prédios. Não substitui teste dentro do jogo. Unity, Play Mode, build, capturas, commit, push e desligamento não executados; temporários de compilação/teste removidos ao concluir.

Verificação do acesso de 09/11: cena independente e prefab reabertos, uma chave de biblioteca com 33 pontos, zero chaves de cela ativas e manutenção preservada. Componentes/IDs e sprites conferidos na Unity em modo de edição; sem nova coleta/alarme em Play Mode. Resultados completos na [ficha da biblioteca](CampusLibrary.md#verificação-do-acesso--09112026).

## Aviso de proximidade da manutenção — 09/11/2026

`LawSchoolMaintenanceAccess` usa o collider sólido já referenciado em `entranceBarrier` para medir a distância ao jogador em mundo. Sem `law-school-maintenance`, exibe o texto exato **“A porta está trancada”** a até 2,5 m. Considera o centro do `CharacterController`, com fallback para o transform do player. O aviso fica oculto com chave, à distância, em pausa/menu, durante morte/carregamento, em outra cena ativa ou ao desabilitar o componente. Caixa de texto alinhada ao padrão de avisos existente, sem novo Canvas ou textura.

Somente o campo `warningDistance` foi acrescentado aos componentes existentes na cena jogável e no prefab; IDs e referências preservados. Porta, barreira, distribuição da chave e alarme não foram alterados. Compilação C# isolada de `Assembly-CSharp` passou sem erros, com avisos preexistentes; referências e campos serializados conferidos. Sem iniciar Unity, abrir cenas, executar jogo/Play Mode, build ou capturas. Exibição e aproximação finais precisam do teste do usuário.
