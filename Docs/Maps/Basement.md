# Porão e prisão — ficha rápida

## Leitura rápida

- **Skill de invocação do Diretor — 12/11/2026:** `DirectorBossSummon` no prefab do chefe agenda cinco zumbis por faixa de 200 pontos de vida perdida enquanto ele vive. Cada zumbi surge junto de um portal animado e sobe até a posição normal; `EnemySpriteAnimator.VerticalOffset` mantém a subida mesmo com a atualização de posição do sprite em `LateUpdate`. Se faltarem cinco posições livres, a onda aguarda e tenta novamente. As animações de conjuração e portal usam `Boss/Textures/DirectorSummonCast.png` e `DirectorSummonPortal.png`; o inimigo reutiliza `Global/Prefabs/Enemy/Enemy.prefab`. Os dois PNGs foram recuperados do Git LFS com tamanho e SHA-256 conferidos, pois o checkout continha somente ponteiros. As 16 referências de sprite do prefab foram conferidas com os IDs dos importadores. A ferramenta temporária `Editor/Temporary/DirectorSummonSetup.cs` preserva esses IDs ao ser reaplicada. Na derrota ou desativação do chefe, a skill encerra as corrotinas e remove portais e invocados. `git diff --check` passou; compilação Unity, Play Mode, build e comparação visual ainda não executados nesta revisão.

- **Caixa de munição da arena — 11/11/2026:** uma instância de `Global/Prefabs/Weapons/UACAmmoCrate50.prefab` foi colocada perto da entrada da sala do chefe, em `(79; 1,73; -20)`, antes do ponto de surgimento do Diretor. A caixa usa a arte baseada na referência enviada pelo usuário e soma **50 munições à reserva da arma equipada** quando coletada. Cena e prefab salvos no Unity 6000.5.7f1; coleta e aparência em Play Mode ainda não verificadas.

- Chave avulsa da prisão retirada do gameplay a pedido do usuário: raiz de `Prison/Prefabs/PrisonKey.prefab` inativa. A instância no prefab principal herda esse estado pela cadeia `Basement.unity` → `BasementScenario.prefab` → `Basement.prefab`. Referências e herança conferidas estaticamente; inspeção em jogo pendente.

- Arte e entrada do porão (10/11/2026): importadores afetados por GUIDs regenerados foram recuperados, incluindo professores e recursos compartilhados. A transição deixou de aquecer globalmente shaders internos da Unity, caminho com falhas no log anterior ao crash. Layout, celas e progressão não foram revertidos; a entrada precisa do teste do usuário. [Diagnóstico compartilhado](README.md#recuperação-de-texturas-e-sprites--10112026).
- Cena: `Assets/_Project/Scenes/Basement/Basement.unity`.
- **Liberação pelo gerador — 11/11/2026:** as 14 celas ficam trancadas até `LibraryMissionProgress.IsLibraryGeneratorPowered`, após instalar os três fusíveis e concluir o puzzle da CampusLibrary. Depois disso, cada cela abre com `E`. O registro direto de abertura também exige energia. Regra e validação em [Basement-Celas-Pendencias.md](Basement-Celas-Pendencias.md).
- Prefab principal: `Assets/_Project/Scenes/Basement/Prefabs/Basement.prefab`.
- Cadeia carregada pelo jogo: `Basement.unity` → `BasementScenario.prefab` → `Basement.prefab`. A variante tem apenas dois overrides na área da prisão, ambos na continuação da parede do santuário; não sobrescreve o layout corrigido das celas.
- Área das celas: `Assets/_Project/Scenes/Basement/Prison/`.
- Arte da transição: `Assets/_Project/Scenes/Basement/Resources/Textures/UI/Transitions/Basement.png`, derivada da visão real de chegada `FromCampus` e tratada no mesmo padrão cinematográfico de `Cylinder`.
- Quantidade atual: 14 celas e 14 professores.
- Final: ao abrir a 14ª cela, os 14 professores desaparecem juntos e o prompt das celas, preservando a mesma tipografia e cor, é centralizado na tela para orientar o retorno à UNIMAR. O portal `CampusExterior/FromPrison` inicia então a comemoração na portaria, com os 14 sprites saltando, fogos, fade e créditos. `TESTAR FINAL` inicia na prisão após o boss, com todas as celas fechadas e gerador já preparado como reparado na sessão de teste, permitindo validar o fluxo inteiro.
- Chaves: os treze sprites numerados continuam preservados, mas só as cinco chaves de cela 4/5/6/10/11, na biblioteca, permanecem ativas. Os oito pickups antigos de Direito/Bloco 4 foram retirados ou convertidos em duas chaves de acesso à biblioteca; manutenção continua separada. A cela 14 de Bianca já possui ID de progressão, sem chave própria criada/distribuída. A abertura das 14 celas exige o gerador da CampusLibrary reparado e a interação E. Contrato conciliado em [PrisonCellKeys.md](PrisonCellKeys.md).
- Ocupação: exatamente um professor por cela; cada porta referencia somente o professor e a voz da própria cela.
- Módulo compacto: passo de **3,20 m**, com área livre aproximada de **3,00 × 3,48 m**. Sete celas ficam em cada lado; conjunto de 22,40 m ao longo do corredor. Folha móvel de aproximadamente 1,47 m e trecho fixo de aproximadamente 1,41 m, alinhados dentro de cada módulo. Divisórias de 20 cm; corredor central livre de aproximadamente 2,97 m.

## Professores

O mapeamento salvo no prefab principal é:

| Cela | Professor | Voz |
|---:|---|---|
| 01 | Caio | `caio_line.wav` |
| 02 | Fábio | `fabio_line.wav` |
| 03 | Henrique | `henrique_line.wav` |
| 04 | Cesar (prefab legado `Teacher-santi`) | `cesar_line.wav` |
| 05 | Navarro | arquivo de voz ainda não fornecido |
| 06 | Matheus Ferraroni | arquivo de voz ainda não fornecido |
| 07 | Paulinho | `paulinho_line.wav` |
| 08 | Rafa | `rafa.wav` (conteúdo fornecido como `audio_rafa_professor.wav`) |
| 09 | Gustavo Camossi | `gustavo_line.wav` |
| 10 | Matheus Santi | `santi_line.wav` |
| 11 | Moacyr | `moacyr_line.wav` |
| 12 | Querino | `querino_line.wav` |
| 13 | William | `william_line.wav` |
| 14 | Bianca | `bianca_line.wav` |

Cada `PrisonCellGate` referencia o transform do único professor de sua cela. `PrisonController` consulta o `AudioSource` desse transform ao aceitar a abertura; os clipes não tocam ao carregar a cena (`Play On Awake` desabilitado). Não atribua falas de outras pessoas às celas 05 e 06: elas devem permanecer sem clipe até os arquivos corretos serem fornecidos.

Em 09/11/2026, as referências legada (`m_audioClip`) e atual (`m_Resource`) do `AudioSource` foram sincronizadas nos prefabs de Gustavo Camossi, Matheus Santi, Moacyr, Querino e William. Assim, a verificação `AudioSource.clip` usada ao abrir a cela resolve a mesma gravação identificada pelo professor, sem reutilizar falas entre celas. Navarro e Matheus Ferraroni continuam intencionalmente sem clipe.

Na revisão das identidades, a cela 04 foi corrigida de uma duplicação de Matheus Santi para Cesar e passou a usar `cesar_line.wav`. A cela 12 mantém Querino e passou a usar `querino_line.wav`. O caminho legado `Teacher-santi.prefab` foi preservado para manter seu GUID; somente o nome do objeto, o vínculo de áudio e os consumidores foram corrigidos.

Na mesma conferência auditiva indicada pelo usuário, Gustavo Camossi e William passaram a usar respectivamente `gustavo_line.wav` e `william_line.wav`, substituindo as gravações alternativas de nomes semelhantes que estavam ligadas aos prefabs.

Após conferência auditiva do usuário, o conteúdo das seis gravações trocadas foi realocado para os arquivos semânticos usados pelos respectivos prefabs: Matheus Santi recebeu a gravação antes ligada a Querino; Cesar, a de Matheus; Querino, a de Cesar; Gustavo, a de Moacyr; Moacyr, a de William; e William, a de Gustavo. Caminhos, `.meta` e GUIDs foram preservados. `rafa.wav` mantém o conteúdo fornecido como `audio_rafa_professor.wav`. Bianca passou a usar `bianca_line.wav` na cela 14.

As quatorze imagens originais mantêm seu canvas 1536 × 1024 e seus bytes. Cada pose parada usa recorte próprio e pivô nos pés; o PPU mantém altura visual próxima dos demais professores. Bianca usa o recorte `bianca_Idle` de 144 × 336 px, PPU 186,66667 e altura de 1,80 m, com o material compartilhado de remoção do fundo preto. Os demais sprites preservam seus materiais RGB/RGBA anteriores. GUIDs e IDs dos sprites foram mantidos.

## Inclusão da Bianca — 09/11/2026

A Unity importou `Global/Sprites/teachers/sprite-bianca-professora.png` sem alterar os bytes da imagem, criou `Prison/Prefabs/Teacher-bianca.prefab` com `SpriteRenderer`, `Billboard` e `AudioSource`, e reconstruiu o prefab principal. A cela 14 ocupa o módulo direito final, em paralelo à cela 13, sem ampliar o corredor. A validação registrou 14 `PrisonCellGate`, IDs únicos de 1 a 14, `Professor Bianca - Cela 14` e clipe `bianca_line`. O prefab foi validado fora de Play Mode; abertura, áudio e comparação visual dentro do jogo ainda precisam de teste manual.

## Regra de capacidade

Distribua exatamente um professor por cela. Novos professores exigem novo módulo compacto de 3,20 m, porta, `PrisonCellGate`, ID único, placa e referência exclusiva ao professor/áudio. Com quantidade ímpar, um dos lados do corredor pode terminar com uma cela adicional e o lado oposto deve receber fechamento sólido.

## Validações anteriores — histórico

Em 2026-11-09, o prefab foi reorganizado pela Unity para 13 celas compactas. A validação do asset confirmou 13 `PrisonCellGate`, IDs únicos de 1 a 13, 13 referências distintas de professor e os 11 clipes disponíveis ligados às pessoas corretas. Navarro e Matheus Ferraroni não possuem arquivos de voz no projeto nem nos anexos recebidos. Capturas de revisão em 1600 × 900 foram feitas na entrada e no meio do corredor; grades, iluminação, fechamento do lado par, piso, teto e rota central permaneceram legíveis. Compilação final e teste de abertura em Play Mode ainda devem ser registrados após retirar a ferramenta temporária.

Em 2026-11-09, a transição global passou a bloquear o jogador até concluir o aquecimento dos shaders e ganhou arte 16:9 própria para o porão. A captura-fonte foi renderizada na chegada real `FromCampus`, sem alterar cena, prefab, layout ou spawn.

Em 2026-11-09, a Unity importou os cinco sprites, criou os cinco prefabs e confirmou 13 professores no prefab principal. A revisão estática confirmou uma instância de cada professor, GUIDs únicos, referências de sprite e áudio válidas e ausência de fileIDs duplicados. Após a restauração das vozes, o piso de ruído medido passou de aproximadamente −31/−39 dB para −72/−75 dB nos clipes com fundo audível; os picos finais ficaram entre −3,3 e −1,5 dB. A Unity reimportou os cinco WAVs sem erros, mantendo seus GUIDs. O Play Mode do porão e uma comparação auditiva dentro das celas não foram executados após o delta final minimizado.

## Correções da integração de development — 09/11/2026

O log existente `Logs/Editor.log` mostrou `CS0200` em `PrisonPlayValidation.cs`: a ferramenta tentava atribuir `PrisonProgress.HasKey`, propriedade somente de leitura. A ferramenta agora preenche seu inventário de validação com `PlayerCellKeys.TryAddKey`, conforme os IDs das celas. Sua saída automática do editor ficou restrita ao modo batch; não fecha uma sessão interativa.

A expansão havia criado as celas 9–13 mantendo a progressão limitada a oito. Após autorização do usuário, `PrisonProgress.CellCount = 13` passou a definir o limite comum de IDs, gates, pickups e HUD; cinco novas chaves foram distribuídas. O ícone de fallback da HUD reutiliza a chave neutra existente; os novos pickups têm o número na plaqueta.

A cena usa `BasementScenario.prefab`, que instancia `Basement.prefab`. Conferidas a cadeia de referências e a ausência de overrides do ícone. Prefabs recebidos e modificados foram conferidos por IDs/referências; áudios e imagens recebidos contêm dados completos, sem ponteiros LFS pendentes.

Nesta correção, nenhuma Unity, compilação, jogo, Play Mode, build, ferramenta de validação ou captura foi iniciada. O diagnóstico usa o log já existente e inspeção dos arquivos. Sem commit ou push.

## Isolamento das celas e sprites completos — 09/11/2026

Problemas confirmados nos arquivos:

- Nove malhas agrupadas do layout antigo continuavam visíveis junto às treze celas compactas. Havia paredes, grades e sinalização antigas sobrepostas à arquitetura nova.
- A construção compacta posicionava portas e grades como se o pivô estivesse no centro, mas os segmentos se estendem para o lado positivo de Z a partir da dobradiça. A grade fixa ultrapassava o limite da cela seguinte.
- Um mesmo recorte de 126 × 284 pixels era usado em desenhos com posições e tamanhos diferentes. Braços e pés ficavam fora do retângulo; o recorte de Querino começava em x=189, enquanto sua pose começa perto de x=92.
- Os cinco PNGs novos já continham alpha, mas o material compartilhado estava configurado para ignorá-lo, deixando fundos visíveis.

Correções salvas:

- Renderers das nove malhas antigas desativados; assets e IDs preservados. Pisos, paredes externas, teto, passagem, fechamento do lado par e mesa de triagem receberam renderizadores de cubo nas geometrias dos próprios colisores existentes. Materiais anteriores reutilizados. Cinco difusores restaurados junto às luzes existentes. Sem alterar luzes ou colisores dessas superfícies.
- Treze frentes alinhadas ao início de cada módulo. Folha em `startZ + 0,23`, escala Z=0,64; trecho fixo em `startZ + 1,75`, escala Z=0,60. Colisores ajustados às travessas visíveis. As grades deixam de invadir a próxima cela. A abertura foi limitada a 90° para a folha não atravessar a divisória ao terminar o giro.
- Treze professores posicionados em `x = ±3,10`, `y = 0,02`, `z = startZ + 1,10`, com folga para paredes e bancos, um professor por cela. Referências de professor, voz, chave e interação preservadas.
- Treze importadores receberam recortes completos e altura consistente. Cinco prefabs passaram a usar o material que respeita o alpha. Nenhuma imagem original foi substituída ou editada; hashes dos treze PNGs conferidos contra os objetos LFS registrados no Git.
- Ferramenta de layout ajustada para os mesmos offsets e para remover todos os módulos anteriores antes de reconstruir, evitando duplicar celas e placas em uma regeneração futura. A ferramenta não foi executada.

Conferência estática: IDs únicos, referências locais válidas, treze portas/treze professores, folhas contidas nos próprios módulos e envelopes de sprites afastados das paredes. GUIDs, IDs de subassets, vozes, cena e variante preservados. Não houve Unity, Play Mode, compilação, build, captura, vídeo, medição de FPS, commit, push ou desligamento. Aparência, circulação e abertura em execução precisam de conferência do usuário.

## Remoção de móveis e latas abertas — 09/11/2026

Retiradas da área jogável 11 instâncias de estrados/camas e colchões antigos, desativando suas raízes. Os treze bancos retangulares e a mesa de triagem também ficaram inativos. Objetos serializados e assets originais preservados; paredes, pisos, grades, professores, pickups e suas referências não foram alterados nesta etapa. A ferramenta de layout mantém os bancos desativados.

Adicionado `Prison/Prefabs/OpenFoodCan.prefab`: lata de alimento aberta enferrujada, 18 cm de altura incluindo a tampa, 60 triângulos, textura e normal map de 256 × 256. Dezesseis instâncias junto às laterais internas: uma em cada cela, com uma extra nas celas 03, 06 e 09, sem colisores e sem ocupar o corredor ou o espaço dos professores. Malha nativa Unity, sem dependência de Blender instalado.

Modelo **open can (junk)** de **p0ss**, [OpenGameArt](https://opengameart.org/content/open-can-junk), CC0 1.0. Autor incluído em `CreditsContent.txt` e `Docs/Maps/creditos.md`, sem links nos créditos exibidos. Origem, licença, hashes e conversão em `Licenses/AssetSources/Basement/open-food-can.json`.

Conferência estática de referências, IDs, malha e posicionamento. Unity, compilação, Play Mode, build, comparação visual, screenshots e vídeo não executados. Sem commit, push ou desligamento.

## Padronização dos pickups — 09/11/2026

As 11 caixas de munição e os 9 medkits adicionados ao prefab principal foram reduzidos e padronizados. Todas as caixas usam escala `4 × 3 × 1` (altura visual aproximada de 0,33 m), inclusive as duas caixas centrais antes maiores; após a revisão visual, todos os medkits usam `0,09 × 0,09 × 1` (altura visual aproximada de 0,48 m) para não parecerem pequenos demais diante das caixas. O pivô vertical dos kits foi compensado em 0,031 m para preservar o apoio. Posições horizontais, quantidades, valores de munição/cura, prefabs globais, GUIDs e demais mapas foram preservados.

Conferência estática dos 20 overrides de escala e das referências dos prefabs. Unity, compilação, Play Mode, build e comparação visual não executados nesta alteração.


## Portal com vista do portão — 09/11/2026

`Exit to CampusExterior` foi trazido do antigo Z=27,25 (atrás da parede) para Z=26,35 no espaço da prisão. A janela oval mede 2,50 × 3,15 m, fica centrada em Y=1,65 e em Z=26,425, a 2,5 cm da face acessível da parede do registro (Z=26,45). O trigger mede 2,15 × 2,70 × 0,22 m; a chegada `Return from campus` foi preservada. A conexão permanece `CampusExterior/FromPrison`.

`Global/Scripts/Utils/ParallaxPortalView.cs` usa perspectiva assimétrica conforme a posição dos olhos. A prévia persistente `Prison/Prefabs/CampusGatePortalPreview.prefab` reutiliza as malhas/texturas da entrada UNIMAR, com recortes persistentes das três malhas que também continham a porta/antepátio da prisão, materiais de iluminação simples e uma malha persistente para o letreiro. O piso reaproveita a textura de asfalto existente. A prévia final tem 812 triângulos e 11 renderers. Não carrega a cena exterior, gameplay, física ou luzes do campus. A textura é 384 × 512, sem MSAA, mipmaps, HDR, sombras, depth/color copies ou pós-processamento; atualização limitada a 30 Hz, somente com mudança da posição dos olhos, dentro do enquadramento frontal e até 26 m. Prévia isolada na camada 31, com renderers ocultos fora da renderização manual; recursos liberados ao desativar o componente. `SceneTransitionDoor` não adiciona os anéis antigos quando encontra a janela nova.

Validação desta alteração: compilação na Unity 6000.5.7f1, carregamento da cena real com sua cadeia de prefabs, preservação de todos os fileIDs anteriores do prefab principal, capturas centrais/laterais e diferença de pixels entre esquerda/direita. Verificados zero renders extras com câmera parada, portal fora do enquadramento ou além do alcance, e liberação da textura ao desativar. Em Play Mode, confirmados renderização por LateUpdate, atualização após deslocamento lateral do jogador, ausência dos anéis antigos, travessia por contato com o CharacterController até CampusExterior e limpeza da prévia ao descarregar a prisão. Build e perfil de CPU/GPU/FPS não executados; não há ganho de desempenho medido. As capturas de revisão são temporárias, fora de Assets.

A ferramenta existente `PrisonCellLayoutBuilder` preserva a posição de saídas que já possuam `ParallaxPortalView`, evitando deslocar novamente a janela para trás da parede ao reconstruir as celas. Durante o teste de travessia, o aquecimento global `Shader.WarmupAllShaders` em `DoorReloadScreen` emitiu mensagens `Keyword state size mismatch` de shaders URP Lit/Unlit; a travessia concluiu. A origem dessas mensagens não foi investigada e nenhum ajuste na pipeline foi feito nesta tarefa. As ferramentas temporárias criadas para esta integração foram retiradas após verificar assets persistentes e referências; a prévia funciona sem elas.

## Sequência final — 10/11/2026

`PrisonProgress` mantém a condição das 14 celas e marca o início do final somente quando o portal existente do porão aceita a travessia para `CampusExterior`. `PrisonController` oculta os 14 transforms de professor no mesmo frame e mantém o aviso de retorno no HUD. Em `CampusExterior`, `FinaleDirector` reutiliza os 14 sprites compartilhados cadastrados em `Global/Resources/Finale/FinaleCast.asset`, posiciona-os diante da chegada `FromPrison`, bloqueia o movimento durante a comemoração e entrega o controle aos créditos compartilhados depois do fade. `OS PROFESSORES ESTÃO A SALVO` usa o mesmo `MenuStyle` dos menus: fonte padrão da interface em negrito, cor `Paper`, fundo escuro e acento `Rust`. Os fogos são uma sequência procedural com foguetes ascendentes, rastros quentes, estouros em alturas e posições alternadas, núcleo branco, cores em camadas, faíscas secundárias, gravidade, cintilação, fumaça e clarão local curto. Os foguetes mantêm a subida espacializada e a explosão grave procedural com variação de pitch; o aplauso usa o clipe compartilhado `Global/Resources/Audio/Finale/aplausos-professores-final.wav` e obedece ao volume geral. Ao terminar ou pular os créditos, a sessão é encerrada e o menu principal reaparece.

Durante essa sequência, `FinaleDirector.Awake` desativa todos os `Enemy` e `EnemySpawner` pertencentes a `CampusExterior` antes do primeiro `Start`; `EnemySpawner` também possui uma guarda própria para não criar zumbis quando `PrisonProgress.FinaleStarted` estiver ativo. O jogo normal no campus não é alterado.

Validação desta alteração: Unity 6000.5.7f1 importou e compilou os scripts sem erros e criou o asset persistente com 14 referências de sprite. O teste anterior do atalho direto confirmou que `CampusExterior` resolvia o elenco e iniciava os fogos; ele registrou um aviso ao configurar a duração de partículas já ativas, e a criação agora interrompe e limpa cada sistema antes de configurá-lo. A sequência pirotécnica mais realista também compilou sem erros. O novo comportamento de `TESTAR FINAL` — iniciar na prisão com as 14 celas fechadas — ainda não percorreu todo o fluxo em Play Mode. A ferramenta temporária de geração foi removida. A travessia completa, enquadramento visual dos novos fogos, build e comparação visual ainda precisam ser executados.

Em 10/11/2026, o aplauso procedural foi substituído pelo WAV fornecido `aplausos-professores-final.wav` (PCM estéreo, 48 kHz, 13,224 s), preservando os sons procedurais dos fogos. A Unity 6000.5.7f1 importou o clipe e recompilou os scripts sem erros. A sequência completa em Play Mode e a comparação auditiva dentro do jogo não foram executadas após a troca.


## Remoção dos suprimentos das celas — 09/11/2026

A pedido do usuário, removidas do prefab principal 6 instâncias de `Medkit - Cela` e 6 de `Caixa de municao - Cela`, das celas 1–6. Todas as 14 celas ficaram sem kits e munição posicionados. Preservados os suprimentos do corredor de custódia, da arena e do santuário, os prefabs compartilhados, professores, chaves, portas e portal. A ferramenta de layout existente não gera esses pickups.

Verificadas referências serializadas às instâncias antes da remoção. Cena real carregada na Unity 6000.5.7f1 com sua cadeia de prefabs: zero kits/munição dentro das celas, 14 portas e um portal. Compilação/importação executadas; Play Mode, build e comparação visual não executados nesta alteração. Nenhum asset de item compartilhado foi excluído; as instâncias podem ser recolocadas a partir dos prefabs globais. A ferramenta temporária desta tarefa foi retirada.
