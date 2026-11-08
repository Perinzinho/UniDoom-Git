# Campus Exterior e Biblioteca do Campus — contexto de implementação

> Documento para continuar o trabalho sem reconstruir o contexto da conversa nem ler milhares de assets. Atualizado em **07/11/2026**. A ampliação atual de CampusLibrary tem ficha em [Docs/Maps/CampusLibrary.md](Docs/Maps/CampusLibrary.md); registros anteriores abaixo conservam suas datas. Base histórica: `development`, commit `494fdd18`. Alterações futuras podem mudar os números e parâmetros abaixo.

## 1. Leitura rápida para a próxima IA

- **Campus Exterior** é o cenário com gramados, árvores, avenidas, portaria UNIMAR, fachadas e caixa-d'água: cena **`CampusExterior`**.
- A nova biblioteca desse campus tem interior separado: cena **`CampusLibrary`**. Entrar e sair usa a transição existente do jogo, ao tocar no volume da porta.
- **`Library` é a biblioteca antiga do começo do jogo. Não foi substituída pela nova biblioteca.** Não confundir também `CampusExterior` com `EntranceGarden` ou `Main/SampleScene`.
- O exterior e o interior já estão salvos em cenas, prefabs, meshes e materiais persistentes. **Não precisam ser gerados novamente para jogar.**
- O exterior possui **12 vagas de estacionamento** demarcadas em três trechos viários, **12 bancos de concreto** nos três pátios sociais e uma **fonte ornamental exclusiva** no pátio entre o Bloco de Direito e a biblioteca, em `(-58, 0, 16)`.
- A biblioteca foi ampliada em 07/11/2026 de aproximadamente **56 × 24 m para 56 × 42 m**, com leitura silenciosa, doze cabines adicionais, duas salas de estudo e trinta estantes adicionais. Após restaurar a organização de `32028565`, a fila revisou móveis e colisões em **mais 20%**, considerando o player de 2,12 m: cadeiras com 1,0956 m, estantes com 2,58 m e mesas novas a 0,96 m. Dois pares de estantes passaram à lateral norte para separar os sanitários por um vestíbulo; modelos locais, luz e placas também foram revistos. A [ficha da biblioteca](Docs/Maps/CampusLibrary.md) registra medidas e validações atuais; [tarefas](Docs/Maps/CampusLibrary-Tarefas.md) e [puzzles físicos](Docs/Maps/CampusLibrary-Puzzles-Caoticos.md) mantêm a fila e as propostas de gameplay, ainda sem implementação.
- O jogador é persistente entre cenas pelo `GameManager`; não crie outro sistema de player, HUD, inventário ou carregamento para esses ambientes.
- Antes de alterar, leia a seção correspondente abaixo e somente os scripts/assets envolvidos. Não despeje cenas, prefabs e arrays de vértices inteiros no contexto.
- Respeite o [AGENTS.md](AGENTS.md). Preserve alterações locais, GUIDs, fileIDs e `.meta`. Exclusões definitivas exigem autorização explícita.
- Houve um incidente em que `Assets` ficou vazia. Os **8.908 arquivos rastreados foram recuperados**, sem faltar arquivo; **504 assets LFS** foram verificados por SHA-256. **A causa do desaparecimento permanece desconhecida.** Veja a seção 10 antes de interpretar o status do Git.

## 2. Mapa do projeto e identificação correta dos cenários

| Cena | Caminho em `Assets/_Project/Scenes/` | Papel |
| --- | --- | --- |
| `CampusExterior` | `CampusExterior/CampusExterior.unity` | Campus externo com grama, avenidas, árvores e edifícios desta implementação. |
| `CampusLibrary` | `CampusLibrary/CampusLibrary.unity` | Novo interior da biblioteca ligada ao Campus Exterior. |
| `LawSchool` | `LawSchool/LawSchool.unity` | Bloco de Direito ligado ao Campus Exterior; saída controlada pelo alarme interno. |
| `CampusBlock4` | `CampusBlock4/CampusBlock4.unity` | Interior térreo do Bloco 4; salas internas conectadas sem carregamento. |
| `Library` | `Library/Library.unity` | Biblioteca anterior, preservada. |
| `EntranceGarden` | `EntranceGarden/EntranceGarden.unity` | Jardim/entrada anterior; não é o novo Campus Exterior. |
| `SampleScene` | `Main/SampleScene.unity` | Cenário anterior. O menu ainda o apresenta como **CAMPUS COMPLETO**; esse texto não identifica `CampusExterior`. |
| `Basement` | `Basement/Basement.unity` | Porão/prisão; possui a passagem para o Campus Exterior. |
| `Cylinder`, `ClassRoom`, `DirectorRoom` | Pastas de mesmo nome | Ambientes anteriores; o padrão de transição foi reaproveitado. |

**Ambiente registrado:** Unity **6000.5.7f1**, URP **17.5.0**, Cinemachine **3.1.7**, Input System **1.20.0**, AI Navigation **2.0.14**. As fontes de verdade são `ProjectSettings/ProjectVersion.txt` e `Packages/manifest.json`.

As **onze cenas** estão habilitadas em `ProjectSettings/EditorBuildSettings.asset`. Após integrar os dois blocos, `CampusExterior` é a oitava entrada, `LawSchool` a nona, `CampusLibrary` a décima e `CampusBlock4` a décima primeira. Prefira os nomes das cenas a índices fixos. O contexto do Bloco de Direito e sua ligação com o exterior está em [Docs/Maps/LawSchool.md](Docs/Maps/LawSchool.md#conexões).

Na saída de **LawSchool → CampusExterior / FromLawSchool**, o alarme do bloco bloqueia temporariamente a transição. `SceneTransitionDoor` consulta um guard opcional, vinculado somente a essa saída; desligar o quadro da manutenção libera o portal. Os demais portais continuam sem esse guard. Aviso e validação registrados na ficha de LawSchool.

Ao entrar em **LawSchool / FromCampus**, a transição compartilhada mostra a captura do hall em `LawSchool/Resources/Textures/UI/Transitions/LawSchool.png` e o título **BLOCO DE DIREITO**. O reconhecimento da cena, checkpoint e prévia é feito por `Global/Scripts/UI/DoorReloadScreen.cs`; o portal e o spawn ID continuam os mesmos. Validação da entrada real e da restauração do jogo registrada na ficha do bloco.

## 3. Onde editar cada coisa

### Passos por piso (2026-11-06)

O player persistente usa `Global/Scripts/Utils/FootStepController.cs` e o resolvedor `FootstepSurface.cs`. Cada pisada consulta o colisor mais próximo sob o jogador: `Campus asphalt` usa passos secos de pavimento; `Campus continuous ground` usa grama; calçadas, sarjetas e degraus usam concreto; `Granite floor` da CampusLibrary usa o banco interno `Eco`. Assim, meshes visuais separados dos colisores não deixam os pisos silenciosos. Não reativar colisores antigos para adicionar som.

Os novos bancos são carregados em `Resources/Audio/Footsteps/{Grass,Pavement}` dentro de `Assets/_Project/Global/`. Não mover essas pastas sem atualizar `Resources.LoadAll` no controlador. São cinco variações por banco, do pacote Kenney Impact Sounds (CC0); o conjunto `concrete` do autor representa o pavimento seco do asfalto. Bancos antigos de concreto, madeira e interiores continuam no prefab do player. Fontes e hashes: `Licenses/AssetSources/Footsteps/sources.json`.

Para pisos novos, o componente opcional `FootstepSurface` permite definir explicitamente o material sonoro no colisor ou em seu grupo. Sem componente, o resolvedor usa nomes conhecidos, tags legadas e material único do renderer local; o fallback é concreto. Renomear os suportes sem renderer listados acima exige atualizar a regra ou adicionar o componente.

Validação: dez OGG decodificados, com duração/amplitude verificadas e downloads conferidos por SHA de blob; referências/caminhos e diff revisados. Unity indisponível neste ambiente: compilação, carregamento de cenas/prefabs, Play Mode, build e comparação visual/auditiva no jogo não executados. Conferir a travessia rua → calçada → grama, entrada na biblioteca, pausa, aterrissagem e volume em relação aos áudios antigos. Os WAV antigos deste checkout ainda são ponteiros Git LFS; não foram substituídos nesta alteração.

| Necessidade | Arquivo ou pasta |
| --- | --- |
| Exterior jogável, iluminação e objetos salvos na cena | `Assets/_Project/Scenes/CampusExterior/CampusExterior.unity` |
| Prefab de referência do exterior | `Assets/_Project/Scenes/CampusExterior/Prefabs/CampusExterior.prefab` |
| Materiais, meshes, modelos e texturas exclusivos do exterior | Subpastas correspondentes de `Assets/_Project/Scenes/CampusExterior/` |
| Vagas persistentes | `Meshes/StreetParkingBays.asset`, usado por `Street Parking Bays - 12` na cena |
| Banco de concreto dos pátios | `Prefabs/CampusConcreteBench.prefab`, instanciado por `CampusLandscapeCourtyard.prefab` |
| Fonte entre Direito e biblioteca | `Prefabs/CampusOrnateFountain.prefab`, instância `Ornate fountain - Law School and Library` na cena |
| Luz ambiente e névoa do exterior ao trocar de cena | `Assets/_Project/Scenes/CampusExterior/Scripts/CampusExteriorEnvironment.cs` |
| Abertura do portão de entrada | `Assets/_Project/Scenes/CampusExterior/Scripts/CampusEntranceGate.cs` |
| Pós-processamento do exterior | `Assets/_Project/Scenes/CampusExterior/Profiles/CampusDusk.asset` |
| Depósitos de sujeira localizados | `Assets/_Project/Scenes/CampusExterior/Shaders/LocalizedWeathering.shader` |
| Cena e prefab da nova biblioteca | `Assets/_Project/Scenes/CampusLibrary/CampusLibrary.unity` e `Prefabs/CampusLibrary.prefab` |
| Geometria da nova biblioteca | `Assets/_Project/Scenes/CampusLibrary/Meshes/`, com cadeiras/estantes compartilhadas em `Global/Meshes/CampusInterior/` |
| Paleta compartilhada dos interiores do campus | `Assets/_Project/Global/Materials/CampusInterior/` e `Global/Textures/CampusInterior/` |
| Pós-processamento da biblioteca | `Assets/_Project/Scenes/CampusLibrary/Profiles/CampusLibrary.asset` |
| Imagem de carregamento da biblioteca | `Assets/_Project/Scenes/CampusLibrary/Resources/Textures/UI/Transitions/CampusLibrary.png` |
| Trigger que solicita a troca de cena | `Assets/_Project/Global/Scripts/Utils/SceneTransitionDoor.cs` |
| Identificação e posicionamento da chegada | `Assets/_Project/Global/Scripts/Utils/SceneArrivalPoint.cs` |
| Tela de transição e carregamento | `Assets/_Project/Global/Scripts/UI/DoorReloadScreen.cs` |
| Sessão persistente do jogador | `Assets/_Project/Global/Scripts/Core/GameManager.cs` |
| Seleção de mapas e nomes exibidos | `Assets/_Project/Global/Scripts/UI/GameMenus.cs` |
| Nome do ambiente no pause | `Assets/_Project/Global/Scripts/UI/MouseSensitivityMenu.cs` |
| Atmosfera regional usada pela biblioteca | `Assets/_Project/Global/Scripts/TotalChaos/TCRegionAtmosphere.cs` |

**Cuidado com cena versus prefab:** o exterior contém o root **`Campus Exterior - Part 2`** serializado diretamente na cena, além do prefab `CampusExterior`. Alterar somente o prefab não garante que os objetos equivalentes na cena sejam atualizados. As revisões anteriores trataram ambos. A cena da biblioteca, por sua vez, referencia o prefab `CampusLibrary` como instância. Confira overrides antes de aplicar mudanças.

## 4. Campus Exterior: o que foi implementado

### Organização e ambiente

Os grupos principais no prefab do exterior são:

| Grupo na hierarquia | Conteúdo |
| --- | --- |
| `01 Terrain and lawns` | Base do terreno, gramados e áreas de plantio. |
| `02 Avenues and sidewalks` | Avenidas, calçadas, meios-fios, sarjetas, pintura e remendos do asfalto. |
| `03 Entrance Unimar and gate` | Entrada UNIMAR, guarita, pilares e portão deslizante. |
| `04A Law school exterior` | Fachada do prédio de Direito. |
| `04B Library exterior` | Fachada da biblioteca e acesso à nova cena interior. |
| `04C Block 4 exterior` | Fachada do Bloco 4. |
| `06 Garden furniture and lighting` | Bancos, mobiliário externo e iluminação. |
| `07 Broadleaf groves and palm avenues` | Árvores de copa larga, palmeiras e vegetação. |
| `08 Water tower and western annexes` | Grupo histórico da caixa-d'água e anexos; a revisão viária removeu fragmentos de casas, preservando a torre. O nome do grupo permaneceu. |
| `09 Campus perimeter` | Fechamento, pilares e limites do campus. |

Também existem os grupos de revisão `Reviewed utility lighting`, `Localized water damage` e `Campus maintenance props`, além de pontos de chegada e portais.

A direção visual segue o contexto local em [`.codex/SKILL.md`](.codex/SKILL.md): arquitetura universitária reconhecível, desgaste plausível, materiais distinguíveis e atmosfera de survival horror com navegação legível. O documento local registra a referência visual em Total Chaos; isso não significa que assets ou técnicas do mod foram copiados.

### Ruas, circulação e acabamento

- A malha viária foi revisada como contorno contínuo, com curvas e cruzamentos, em lugar de depender somente de blocos de pista sobrepostos.
- A revisão registra abaulamento suave de **3,5 cm**, sarjeta de **28 cm**, topo do meio-fio a **18 cm** e calçadas de **1,6 m**. São parâmetros da revisão viária, não medidas universais de todos os pisos.
- Foram tratados três pontos de travessia e cinco ligações de pedestres, com rebaixamentos em pontos definidos.
- A superfície visual é dividida em células `RoadCell_*`, `WalkwayCell_*` e grupos semelhantes. Isso preserva a distribuição espacial da iluminação; não una tudo indiscriminadamente em um único renderer.
- As colisões antigas de ruas foram desabilitadas pela revisão; meshes persistentes fornecem a colisão atual de asfalto, calçada, meio-fio e sarjeta. **Não reative colisores antigos em massa**, pois podem duplicar superfícies e degraus.
- Há pintura branca/amarela contida, asfalto com mapas de superfície e remendos localizados, em vez de sujeira uniforme cobrindo todo o campus.
- Telhados, fachadas, vidro, estrutura metálica, vegetação, luminárias e props receberam materiais próprios ou recursos compartilhados existentes.

### Vegetação e desempenho

O prefab atual contém **124 `LODGroup`s** associados às árvores de copa larga, com objetos `Canopy medium`, `Canopy distant` e colisões de tronco. Existem também palmeiras e canteiros compartilhados. Não elimine níveis de detalhe nem substitua todas as copas por versões de maior custo sem medir o resultado.

Inventário exclusivo recontado em 06/11/2026, sem `.meta`: **176 meshes**, **33 materiais**, **3 modelos FBX**, **6 prefabs**, **20 PNGs**, **1 HDR**, **2 scripts**, **1 shader**, **1 perfil** e **1 cena**. Esses números não incluem recursos compartilhados ou conteúdo interno dos modelos importados; não são uma medida de draw calls.

A lixeira `metal_trash_can` passou a ser compartilhada com LawSchool. Prefab em `Assets/_Project/Global/Prefabs/Equipment/metal_trash_can/metal_trash_can.prefab`; mesh, material e texturas correspondentes em `Global/{Meshes,Materials,Textures}/Equipment/metal_trash_can/`. Movimento por `AssetDatabase.MoveAsset`, preservando GUIDs e conteúdo. Dependências da cena CampusExterior por GUID permaneceram equivalentes; não houve mudança de layout ou conexões. Proveniência da reutilização: `Licenses/AssetSources/LawSchool/equipment-sources.json`.

### Portão e iluminação

`CampusEntranceGate` acompanha `GameManager.SessionPlayer`. Ao chegar a menos de **15 m**, abre as duas folhas, com deslocamento local de **5,6 m** e duração de **2,8 s**, usando `SmoothStep` e áudio de abertura. O comportamento atual é abrir uma vez naquela instância da cena, sem lógica de fechamento automático.

`CampusExteriorEnvironment` é necessário na transição de ambientes escuros para o exterior. Ele aplica o ambiente apenas quando a sua cena é a ativa, no callback de renderização da câmera. O componente salvo usa:

- `ambient = (0.45, 0.46, 0.44)` e `haze = (0.16, 0.18, 0.18)`;
- iluminação ambiente `Trilight`, com fatores de céu/equador/chão `0.72 / 0.34 / 0.12`;
- névoa `ExponentialSquared`, densidade **0.0015**;
- atualização do ambient probe somente ao ativar ou mudar a iluminação, sem reconstruí-lo em toda câmera a cada frame.

Não confunda os valores padrão declarados no script com os valores serializados no componente. O perfil `CampusDusk` contém `ColorAdjustments`, `Bloom` e `Tonemapping`. A cena possui sol, preenchimento de céu e iluminação local. O shader `UniDoom/Campus/LocalizedWeathering` faz depósitos transparentes localizados; mantenha o shader e as referências dos materiais.

## 5. Ligações entre cenas: nomes e posições exatos

```text
Basement / prisão  <-->  CampusExterior  <-->  CampusLibrary
                              |
                              +-------------> LawSchool (ida e volta; saída depende do alarme)
                              +-------------> CampusBlock4 (ida e volta)
```

| Origem | `destinationScene` | `destinationSpawnId` | Ponto de chegada na cena de destino |
| --- | --- | --- | --- |
| Saída da prisão, no prefab `Basement` | `CampusExterior` | `FromPrison` | `Arrival from prison` |
| `Return to prison`, no exterior | `Basement` | `FromCampus` | `SceneArrivalPoint` com ID `FromCampus`, no prefab `Basement` |
| `Library entrance - CampusLibrary`, no exterior | `CampusLibrary` | `FromCampusExterior` | `Arrival - FromCampusExterior` |
| `Exit to Campus Exterior`, na biblioteca | `CampusExterior` | `FromCampusLibrary` | `Arrival - FromCampusLibrary` |
| Entrada do Bloco de Direito, no exterior | `LawSchool` | `FromCampus` | Chegada do hall no Bloco de Direito |
| Saída do Bloco de Direito, liberada pelo quadro de manutenção | `CampusExterior` | `FromLawSchool` | Chegada do Bloco de Direito no exterior |
| `Block 4 entrance - CampusBlock4`, no exterior | `CampusBlock4` | `FromCampusExterior` | `Arrival - FromCampusExterior`, no Bloco 4 |
| `Exit to Campus Exterior`, no Bloco 4 | `CampusExterior` | `FromCampusBlock4` | `Arrival - FromCampusBlock4` |

Referências de posição salvas nos prefabs, em coordenadas locais dos respectivos roots:

| Objeto | Posição `(x, y, z)` |
| --- | --- |
| Exterior: chegada `FromPrison` e `SpawnPoint` da cena | `(78, 2.45, -65)` |
| Exterior: trigger de retorno à prisão | `(78, 1.55, -71.5)` |
| Exterior: trigger de entrada na biblioteca | `(-43, 2.05, 70.55)` |
| Exterior: chegada `FromCampusLibrary` | `(-43, 1.9, 69.35)` |
| Biblioteca: chegada `FromCampusExterior` e `SpawnPoint` | `(0, 1.75, -9.6)` |
| Biblioteca: trigger de saída | `(0, 1.9, -11.48)` |

As posições dos pontos de chegada são do **root do jogador**, não da câmera nem da superfície do chão. Ao mover portas, ajuste trigger, chegada, piso e orientação em conjunto. Os portais entre campus e biblioteca atualmente não exigem chaves nem aguardam uma folha física abrir.

### Contrato do sistema existente

1. `SceneTransitionDoor` usa `BoxCollider` de trigger e procura `PlayerMovement` no objeto que entrou ou em seus pais. Jogador morto ou transição em andamento não inicia outra passagem.
2. Os campos `destinationScene` e `destinationSpawnId` devem ser válidos; chaves e estado de porta física também podem ser exigidos em outros portais do jogo.
3. `DoorReloadScreen.TryLoadScene` valida o destino no Build Settings, cobre a tela, congela movimento/entrada, carrega com `LoadSceneAsync(..., Single)`, posiciona o jogador e restaura os estados ao finalizar.
4. `SceneArrivalPoint` encontra o ID na cena de destino, usa `Teleport`, sincroniza a física e reposiciona a orientação da Cinemachine. Há fallback para `SpawnPoint`, mas um ID incorreto ainda é erro de configuração.
5. `GameManager` mantém a sessão com `DontDestroyOnLoad`, descarta managers duplicados e preserva o mesmo jogador/HUD durante as transições. Não reinicie vida, munição ou inventário ao entrar na biblioteca.
6. A proteção contra retorno imediato depende de **sair do trigger antes de usá-lo novamente**: entradas durante um carregamento não são enfileiradas. Preserve essa regra ao alterar `OnTriggerEnter/Stay` ou o ponto de spawn.

**Imagem e rótulos:** a nova biblioteca usa `BIBLIOTECA DO CAMPUS` e a chave de recurso **`Textures/UI/Transitions/CampusLibrary`**, carregada por `Resources.Load<Texture2D>`. O caminho é relativo a uma pasta `Resources`, sem extensão. `DoorReloadScreen`, `GameMenus` e `MouseSensitivityMenu` conhecem `CampusLibrary`. Não renomeie essa pasta ou recurso sem atualizar consumidores. O exterior não tem uma entrada própria na lista atual de `DestinationArtwork`; destinos sem arte específica usam a imagem compartilhada.

## 6. Biblioteca do Campus: interior implementado

### Referência, layout e conteúdo

A referência foi o infográfico **“Infográfico da Biblioteca UniDoom.png”** fornecido pelo usuário na conversa. O arquivo original estava fora do repositório, na Área de Trabalho; ele não é uma dependência necessária para abrir a cena. A interpretação implementada é um edifício térreo com desnível interno, e não uma nova biblioteca de múltiplos andares.

- Planta original de aproximadamente **56 × 24 m**, ampliada em 07/11 para aproximadamente **56 × 42 m**, com entrada pela frente em `z` negativo.
- Hall de entrada, atendimento e espera no patamar superior, a **0,64 m** do nível inferior.
- **Quatro degraus de 0,16 m**, com rota tátil azul e corrimãos, conectando hall e leitura/acervo.
- Recepção com balcão, trecho rebaixado, computador de atendimento e quadro de avisos.
- Área de leitura e estudo em grupo à esquerda, com mesas retangulares e redondas.
- **Seis posições individuais de estudo** no patamar de entrada e **doze novas cabines** na ala norte: **18 posições individuais**.
- **Oito computadores de pesquisa**, mais um de atendimento: **nove gabinetes** no total.
- **62 estantes completas** e **95 cadeiras de leitura** após a ampliação; antes eram 32 estantes e 51 cadeiras.
- Dois banheiros, com **seis cabines no total**, bancadas, cubas, divisórias e acessórios.
- Piso de granito, teto modular, vigas alaranjadas, pilares, luminárias, sinalização, plantas, extintores, lixeiras e detalhes de uso universitário.

A geometria persistente está no prefab e nos meshes; a biblioteca não possui um gerador de layout necessário em runtime. O inventário histórico da biblioteca era de **252 meshes**, **24 materiais**, **1 perfil**, **1 prefab**, **1 cena** e **2 PNGs**, além dos `.meta`. Em 06/11, os 24 materiais e `GraniteSpeckle.png` passaram para `Global` para uso compartilhado com o Bloco 4, sem alterar bytes, GUIDs ou importadores. Sete meshes de cadeiras e estantes também passaram para `Global/Meshes/CampusInterior/` com os mesmos GUIDs e bytes. Com o quadro da recepção, a biblioteca conserva **247 meshes exclusivos**, **1 material fotográfico exclusivo**, **1 fotografia PNG**, perfil, prefab, cena e arte de carregamento.

### Ampliação universitária — 07/11/2026

A nova ala persistente **`North wing - university library`** ocupa `x = -28…28`, `z = 12…30.15`, no nível inferior existente. A antiga parede de fundo recebeu passagem central de **6 × 3,20 m**, com soleira no mesmo nível do piso. Não houve mudança dos portais, spawn IDs, hall, quatro degraus ou sanitários.

A ampliação acrescenta **seis mesas de leitura silenciosa**, **duas salas de estudo com uma mesa cada**, **doze cabines individuais**, **44 cadeiras**, **quinze conjuntos de estantes duplas** e um carrinho de livros consultados. Acervo novo dividido por assunto, com corredores laterais de cerca de **3,26 m** e transversais de **1,70 m**. A área bruta nominal cresceu aproximadamente **76%**, sem esticar o player ou os modelos.

O player global mede cerca de **2,121 m** em mundo, com olhos a aproximadamente **1,839 m dos pés**. As 51 cadeiras anteriores e as novas usam altura visual de **0,913 m**, em vez de 0,83 m, com colisões correspondentes e apoio no piso. Mesas novas têm tampo a **0,80 m**; estantes mantêm **2,15 m**. Medidas, planta, dependências e limites da validação estão em [Docs/Maps/CampusLibrary.md](Docs/Maps/CampusLibrary.md).

Reutilizados meshes, materiais e texturas de `Global/*/CampusInterior/`; as superfícies novas ficam em `CampusLibrary/Meshes/NorthWing*.asset`. Não foram baixados novos modelos 3D. Autores já creditados — **Ulan Cabanilla, John Malcolm e James Ray Cock / Poly Haven, CC0** — foram associados à reutilização nos três MDs de créditos e em `Licenses/AssetSources/CampusLibrary/expansion-sources.json`. O inventário de 247 meshes exclusivos citado anteriormente representa a base anterior à ampliação.

### Quadro da recepção

A fotografia original `Screenshot_1.png`, enviada pelo usuário, foi incorporada sem alterar seus bytes em `CampusLibrary/Textures/Campus library portrait.png`. O quadro **`Reception portrait easter egg`** fica na parede frontal interna, junto à recepção, em `(-11.2, 2.45, -11.94)`, voltado para o hall (Y = 180°). A foto tem 1,6 m de largura, preserva a proporção original e usa moldura de madeira compartilhada com os interiores do campus. A linha de visão desde o hall de entrada está livre. Não há legenda ou interação nova.

O material exclusivo fica em `CampusLibrary/Materials/Campus library portrait photograph.mat`; essa pasta contém o novo material da foto, enquanto a paleta anterior continua em `Global/Materials/CampusInterior/`. Os dois meshes novos são `Library portrait plane` e `Library picture frame cube`. A decoração está salva no prefab, não acrescenta colliders e não altera a biblioteca inicial `Library`. Prévias: `output/CampusLibrary-Quadro-Recepcao.png`, `CampusLibrary-Quadro-Detalhe.png` e `CampusLibrary-Quadro-Entrada.png`.

### Revisão de contato e circulação já entregue

O usuário aprovou o ambiente e depois apontou peças próximas demais e detalhes flutuando. A revisão tratou esses pontos sem refazer o cenário inteiro:

- Mesas, cadeiras e respectivas colisões foram reposicionadas em conjunto, afastando-as dos pilares e abrindo as passagens.
- Referência das colunas de mesas revisadas: `x = -21.8, -15, -7.8`; linhas em `z = -1.5, 2.5, 6.5`. A disposição varia conforme o tipo de mesa; não trate esses valores como um gerador universal.
- Pés de mesas retangulares, base das mesas redondas e apoios da longarina foram ajustados ao piso. Os pés das mesas tinham cerca de **4 cm** de afastamento.
- Os nove gabinetes, antes cerca de **11 cm** acima do piso, foram apoiados corretamente; periféricos e livros foram alinhados às superfícies.
- Foram acrescentados apoios ao trecho rebaixado da recepção e às bancadas dos banheiros.
- Placas e luminárias receberam fixações; sinalização de parede, bases de corrimão, vigas, teto e relevos táteis foram alinhados.
- Portas entreabertas das cabines tiveram dobradiças e puxadores alinhados à folha. Foram corrigidos pés de divisórias, rolos de papel, conexões dos sanitários e bases.
- Quadro de avisos, moldura, cortiça e papéis tiveram seus contatos ajustados. Foi acrescentada colisão a um armário.

O grupo **`Contact and clearance details`** e os meshes **`Contact supports ...`** fazem parte dessas correções. Não são sobras de ferramentas. A revisão preservou os **3.108 IDs serializados originais** do prefab e acrescentou **52 IDs**; foram publicados **78 arquivos existentes alterados e 8 novos arquivos de assets**, estes últimos correspondendo a quatro meshes e seus `.meta`. As prévias também foram atualizadas.

**Geometria combinada:** vários objetos visuais estão em meshes `Architecture ...` e `CampusLibrary_<célula>_<material>`, enquanto outros objetos fornecem colisão ou são instâncias de mobiliário. Antes de mover somente um Transform, confira se a superfície visível está combinada em outro mesh. Caso contrário, a colisão pode se deslocar sozinha ou o objeto parecer continuar no lugar antigo.

## 7. Reutilização, dependências e ferramentas

Os dois cenários reutilizam player, arma, HUD, mira, interface de chaves, fonte TMP e scripts globais. O shader compartilhado principal é:

`Assets/_Project/Global/Shaders/TotalChaos/TotalChaosInspiredSurface.shader`

Principais famílias de recursos compartilhados identificadas nas referências:

- Exterior: `Global/Models/Vegetation/CampusBroadleaf.fbx`, prefabs `CampusPalm` e `CampusFernBed` em `Global/Prefabs/EntranceGarden/CampusReference/`, texturas de grama em `Global/Textures/CampusNight/grass_ground/` e materiais/texturas existentes de fachada, reboco e metal.
- Biblioteca: madeira e piso em `Global/Surface/ClassRoom/Textures/`; reboco em `Global/Textures/Library/CampusFinish/`; texturas de livros, cadeiras e estantes em `Global/Textures/Library/`.
- O fato de uma textura ter `Library`, `ClassRoom`, `EntranceGarden` ou `Basement` no nome do caminho compartilhado não a torna exclusiva dessas cenas nem descartável.
- As fontes/licenças do exterior ficam em `Licenses/AssetSources/CampusExterior/`: `sources.json`, `visual-revision-sources.json`, `roof-sources.json` e `road-revision-sources.json`. Há também fontes compartilhadas de `CampusEntrance` e `Library/CampusFinish`. Consulte esses registros antes de substituir ou redistribuir assets.

Existe a ferramenta anterior **`Assets/_TemporaryMenuCapture/Editor/CampusRoadRevision.cs`**, com inspeção, aplicação da revisão, validação, captura e build. Ela usa pedidos em `%TEMP%/UniDoomCampusRoads/request.txt`. **A existência de um método de teste/build não prova que ele foi executado.** Não acione `apply`, `cleanup` ou regeneração automaticamente. Essa ferramenta é de Editor, não um componente de gameplay.

Na implementação/revisão da biblioteca foram usadas ferramentas temporárias fora dos assets finais e o projeto isolado preexistente `.utmp/DirectorRoomValidation`. Esses caminhos não são dependências distribuídas do jogo. Relatórios e ferramentas em `Temp` podem desaparecer ao fechar a Unity; não recrie diretórios de auditoria, geradores, backups ou quarentenas permanentes para compensar isso.

## 8. Validações: o que foi feito e o que não foi

### Ampliação de CampusLibrary — 07/11/2026

A versão ampliada foi salva e reaberta na Unity 6000.5.7f1 / URP 17.5.0: **zero scripts ausentes, zero materiais inválidos, 62 estantes e 95 cadeiras**. **472 GUIDs resolvidos**, **3.186 fileIDs originais preservados**, sete meshes compartilhados e seis arquivos protegidos conferidos por SHA-256. As 18 rotas de NavMesh temporário passaram com agente de 2,141 m e raio de 0,27 m, inclusive chegada à ala nova, salas e cabines.

Play Mode controlado com o player real confirmou cápsula de **2,121 m**, olhos a **1,838885 m dos pés**, travessia da passagem com `CharacterController.Move` e uma instância de jogador. Foram revistas oito capturas em 1920 × 1080, incluindo câmera na Head real. Essa captura de escala não valida input nem acompanhamento automático da câmera. `git diff --check` passou no delta rastreado da tarefa.

**Não houve build, benchmark, percurso manual completo, teste auditivo ou nova ida/volta pelos portais e menus.** O Console já tinha a falha da ferramenta automática de estacionamento; no teste aditivo houve aviso de dois AudioListeners. Nenhum desses sistemas foi alterado nesta revisão. Evidências, limites e capturas estão na [ficha CampusLibrary](Docs/Maps/CampusLibrary.md#validação-desta-revisão); os testes históricos abaixo continuam separados dos atuais.

### Histórico da biblioteca, antes do incidente de exclusão

As seguintes verificações foram executadas durante a implementação/revisão, em uma **cópia isolada na Unity**, conforme registrado nesta conversa:

- Compilação, carregamento de cena/prefab e inspeção de referências.
- Inspeção visual de prévias na altura do jogador e de detalhes próximos.
- NavMesh **temporário de teste**, gerado a partir das colisões salvas, com agente de raio **0,27 m** e altura **2,15 m**: **16 rotas** da entrada às áreas de estudo/pesquisa e às seis cabines passaram. Esse NavMesh de validação não foi publicado como asset de navegação.
- Play Mode com o jogador persistente: toque na porta externa, carregamento da biblioteca, imagem correta, chegada, descida/subida dos quatro degraus e verificações de passagem/contato.
- Saída e reentrada sem player duplicado nem retorno automático; manutenção de vida e munição.
- Conferência de piso para os nove gabinetes e de suporte das bancadas.
- Preservação dos IDs originais do prefab e dos GUIDs existentes. A revisão de contato não alterou a biblioteca antiga nem o exterior protegido naquela etapa.
- `git diff --check` passou naquela entrega.

**Não houve geração de um novo executável na entrega da biblioteca/revisão.** Esses testes históricos não substituem testar novamente depois de alterar ou restaurar arquivos.

### Exterior e estado após a recuperação

Em **05/11/2026**, após a implementação das vagas, bancos e fonte, a Unity recompilou a ferramenta sem erros de C#. A validação específica confirmou o mesh de **12 vagas**, o prefab de banco com **6 renderers**, **12 instâncias de bancos resolvidas** e a fonte ornamental com **31 renderers** na posição esperada. A cena foi salva fora do Play Mode. Não foram executados Play Mode, navegação completa, build nem comparação visual no ponto de vista do jogador nesta alteração.

O código de `CampusRoadRevision.Validate()` permite conferir scripts/materiais/shaders ausentes, fragmentos fora da região preservada da torre e amostras de piso nas principais rotas, detectando falta de chão e saltos maiores que 30 cm. **Nesta documentação, esse validador e o build não foram executados novamente.** As prévias existentes são referência visual histórica, não evidência de um novo teste.

Depois da recuperação de `Assets`, foi confirmada a presença de todos os **8.908 arquivos rastreados** e a integridade SHA-256 dos **504 assets LFS dentro de `Assets`**, totalizando aproximadamente **1,87 GB** de dados conferidos. **Não foram executados novamente Play Mode, validação de cenas/prefabs, comparação visual, compilação deliberada ou build na cópia principal após a recuperação.** A criação deste README não altera esse estado de validação.

### Roteiro curto para uma próxima alteração

1. Abra `CampusExterior` e confirme gramado, iluminação, ruas, portão e pisos das rotas principais.
2. Atravesse a saída da prisão e o retorno, verificando os IDs da seção 5.
3. Toque na entrada da biblioteca: confirme a arte, o nome do destino e o ponto de chegada.
4. Percorra degraus, mesas, estantes, computadores e ambos os banheiros com o player real.
5. Saia e entre novamente; verifique player/HUD únicos, vida e munição, sem loop de transição.
6. Reabra as cenas/prefabs salvos, confira console e alterações no Git; faça build apenas se ele fizer parte da entrega e informe o resultado real.

## 9. Prévias e histórico versionado

As imagens em `output/` são referências para inspeção, não assets carregados pelo jogo. A arte de loading usada pelo jogo está em `Resources`, conforme a seção 3.

| Conjunto | Arquivos em `output/` |
| --- | --- |
| Exterior geral e portão | `CampusExterior-Geral.png`, `CampusExterior-Portao.png` |
| Exterior e árvores após otimizações | `CampusExterior-Otimizado.png`, `CampusExterior-Arvores-Otimizadas.png` |
| Exterior revisado | `CampusExterior-Revisado-Entrada.png`, `CampusExterior-Revisado-Direito.png`, `CampusExterior-Revisado-Escadas.png` |
| Biblioteca: ambientes | `CampusLibrary-Entrada.png`, `CampusLibrary-Leitura.png`, `CampusLibrary-Acervo.png`, `CampusLibrary-Pesquisa.png`, `CampusLibrary-Banheiros.png` |
| Biblioteca após revisão de contato/circulação | [CampusLibrary-Revisada-Mesas.png](output/CampusLibrary-Revisada-Mesas.png) |

Commits úteis para investigação pontual, sem precisar ler todo o histórico:

- `3da2263e` — criação do Campus Exterior e assets relacionados.
- `97e566c3` — revisão visual, modelos detalhados e ruas.
- `50bba1ff` — criação da biblioteca do campus e integração.
- `494fdd18` — revisão da biblioteca e prévias. **Apesar de o título mencionar imagens, o commit contém 92 arquivos alterados/adicionados, incluindo assets da revisão; não trate seu escopo como sendo somente PNGs.**

## 10. Incidente de Assets vazia e interpretação do Git

### Fatos verificados em 05/11/2026

Após os commits da biblioteca, `Assets` ficou vazia e o Git passou a apresentar **8.908 exclusões locais não staged**. Os commits `50bba1ff` e `494fdd18` **não continham exclusões**, e `development` estava sincronizada com `origin/development` na inspeção. O log de encerramento da Unity não estabeleceu a causa. **Não foi demonstrado que fechar o jogo, a Unity ou fazer push tenha causado a exclusão.**

A recuperação foi feita a partir do `HEAD` com `git restore --source=HEAD --worktree -- Assets`, após verificar o estado local. Esse comando fica aqui como registro histórico: **não o execute automaticamente em trabalhos futuros**, pois pode sobrescrever alterações locais legítimas. A recuperação não incluiu reset de branch, novo clone, novo commit ou exclusão dos arquivos novos.

### Quatro arquivos novos criados pela Unity enquanto Assets estava vazia

| Novos na raiz de Assets | Originais já versionados |
| --- | --- |
| `Assets/DefaultVolumeProfile.asset` + `.meta` | `Assets/_Project/Global/Settings/DefaultVolumeProfile.asset` + `.meta` |
| `Assets/UniversalRenderPipelineGlobalSettings.asset` + `.meta` | `Assets/_Project/Global/Settings/UniversalRenderPipelineGlobalSettings.asset` + `.meta` |

As cópias tinham GUIDs diferentes dos originais. O Global Settings novo referenciava somente o Volume Profile novo; as configurações do projeto continuavam usando os originais em `Global/Settings`. Após conferir referências em `Assets`, `ProjectSettings` e `Packages`, nomes usados em código e metadados de bundles, o usuário **autorizou explicitamente remover esses quatro duplicados**. A remoção foi realizada; os originais e seus GUIDs foram preservados. A tabela acima registra os caminhos históricos dos arquivos removidos, não arquivos que precisam ser recriados. Não aplique essa autorização a outros assets.

### Diferenças que não significam mudanças no jogo

- Após restaurar, **30 arquivos de texto** apareciam modificados, sendo **19 com bytes idênticos ao blob do commit** e **11 com diferenças apenas de CRLF/LF**, compatíveis com a conversão normal de checkout. O índice ainda guardava metadados anteriores à recuperação. Após confirmar a igualdade do conteúdo normalizado, `git add --renormalize` foi aplicado **somente a esses 30 caminhos**, recalculando os mesmos blobs e atualizando o índice: **nenhuma alteração ficou staged**. Não faça isso indiscriminadamente com arquivos que possam conter mudanças reais.
- Oito PNGs apresentavam inconsistência entre as regras LFS e os blobs binários comuns já presentes no commit: `RemainingLives.png` e os sete `sprite-*-professor.png` de `Global/Sprites/teachers/`. Seus bytes foram comparados com o commit e eram idênticos. O `.gitattributes` agora possui **oito exceções explícitas `-filter binary`**, mantendo essas imagens como os blobs comuns que já eram. O LFS continua ativo para os demais assets. Remova essas exceções somente se houver uma migração explícita das imagens para LFS; não recodifique as imagens nem ignore mudanças futuras para limpar o status.
- O Git pode chamar arquivos existentes de **“Changes not staged for commit”**; isso não significa que sejam novos. Arquivos realmente não rastreados aparecem com `??` no `git status --short`.
- Desativar filtros LFS em uma inspeção pode fazer binários hidratados parecerem modificados em relação aos ponteiros do Git. Não use esse resultado isoladamente para concluir que texturas/modelos mudaram.
- `.gitignore` já ignora `.utmp/`, `Library/`, `Temp/`, `Logs/`, `UserSettings/` e outras saídas normais da Unity. **A pasta `Library/` da raiz é cache; `Assets/_Project/Scenes/Library/` é conteúdo do jogo.**

Essa limpeza eliminou os 42 avisos originais: 30 entradas com metadados antigos no índice, oito imagens com filtros incompatíveis e quatro arquivos novos duplicados. As mudanças reais de configuração e documentação ficam para revisão em `.gitattributes` e neste README; nenhum conteúdo de gameplay ou imagem foi alterado. Não foram usados `assume-unchanged`, `skip-worktree`, regras de ignore para esconder assets, commit automático ou push. A causa original da exclusão de `Assets` continua sem confirmação. Um novo clone não foi realizado.

## 11. Como manter este contexto útil e economizar tokens

1. Comece pelas seções 1–3. Para portas, leia a 5; para layout da biblioteca, a 6; para status estranho do Git, a 10.
2. Use `rg` com caminho e termo específicos. Faça inventários por tipo/contagem antes de listar milhares de arquivos. Em status volumoso, resuma códigos e diretórios em vez de imprimir todo o diff.
3. Evite ler `Library/`, `.utmp/`, logs completos, binários e arrays de vértices sem uma dúvida concreta que exija isso.
4. A cena, o prefab e os scripts atuais prevalecem se divergirem deste documento. Atualize aqui os nomes de cena, spawn IDs, dependências ou decisões que mudar; não perpetue números antigos como obrigação de recriar assets.
5. Não recrie a biblioteca antiga, não substitua o campus anterior e não regenere o exterior para corrigir um pequeno prop. Preserve o escopo pedido pelo usuário.
6. Salve recursos exclusivos na pasta do cenário e reutilize os compartilhados em `Global`. Mantenha ferramentas pontuais fora dos assets finais e resultados persistentes dentro do projeto.
7. Informe sempre quais verificações foram realmente executadas na alteração atual. Diferencie resultados históricos, inspeção de código, recuperação de arquivos e testes no jogo.


## 12. Bloco 4 — interior conectado ao campus

A planta vigente segue o **segundo desenho enviado pelo usuário em 06/11/2026**: entrada frontal, um corredor principal até o fundo e **dois corredores transversais** cruzando esse eixo. O infográfico inicial permanece como referência de materiais e ambientes. O esquema da implementação está em [output/CampusBlock4-Planta.png](output/CampusBlock4-Planta.png). As referências de planta não são dependências do jogo; a fotografia enviada posteriormente foi incorporada como textura exclusiva deste cenário.

- Cena: `Assets/_Project/Scenes/CampusBlock4/CampusBlock4.unity`; prefab: `Prefabs/CampusBlock4.prefab` dentro dessa pasta. Root: `Campus Block 4 Interior`.
- Planta térrea de **70 × 68 m**. Frente em `z = -34`; corredor principal em `x = 0`, com aproximadamente 6,3 m de largura; cruzamentos em `z = -12,4` e `z = 12,4`, com 3,6 m de largura.
- Quatro alas de **seis salas cada**, com três portas de cada lado. As portas dos pares opostos têm a mesma coordenada `x`: as salas ficam de frente umas para as outras. Salas 401–424 acessíveis sem troca de cena; a 423 exige resolver o enigma das fotografias descrito abaixo.
- Secretaria de Informática à esquerda de quem entra. Bebedouro à esquerda do trecho central; **parede fechada com o quadro da turma à direita**. Escada, elevador e suas placas foram substituídos a pedido do usuário; não há acesso técnico nesse trecho.
- Catracas **na entrada**, em `z = -28`, após a chegada do jogador e antes do primeiro cruzamento, com passagem central livre de 1,8 m. Bancos de espera permanecem dos dois lados do fundo. Quatro banheiros, um no fim de cada ala, com três cabines cada. Os banheiros da esquerda são femininos e os da direita masculinos.
- O espaço atrás da secretaria é **inacessível**, com volumes sólidos fechando a faixa oeste e as antigas brechas frontal e traseira. A porta e o interior da secretaria continuam acessíveis. Os bancos e plantas que ficavam nesse espaço foram reposicionados na espera do fundo.
- Em frente ao bebedouro, a foto da turma de cowboy ocupa um quadro com moldura de madeira e luminária, sobre uma parede contínua com colisão.
- Branco institucional, laranja nas alas direitas, caixilhos escuros, portas azuis, piso tátil, forro modular e luminárias. Mantida a direção visual de desgaste localizado do campus.

### Salas de ADS e Ciência da Computação

| Ala | Primeira fileira | Fileira oposta |
| --- | --- | --- |
| 1 — primeiro cruzamento, esquerda | 401 Algoritmos e Lógica; 402 Programação I; 403 Arquitetura de Computadores | 404 Engenharia de Software; 405 Desenvolvimento Web; 406 Redes de Computadores |
| 2 — primeiro cruzamento, direita | 407 Banco de Dados; 408 Programação Orientada a Objetos; 409 Sistemas Operacionais | 410 Matemática Discreta; 411 Interação Humano Computador; 412 Computação Gráfica |
| 3 — segundo cruzamento, esquerda | 413 Estruturas de Dados; 414 Desenvolvimento Mobile; 415 Segurança da Informação | 416 Robótica e Automação; 417 Sistemas Embarcados; 418 Inteligência Artificial |
| 4 — segundo cruzamento, direita | 419 Análise de Sistemas; 420 Qualidade e Testes de Software; 421 Cloud e DevOps | 422 Projetos Integradores; 423 Pesquisa e TCC; 424 Auditório TEC |

Os ambientes variam entre carteiras para aulas, laboratórios de computadores, mesas em grupos, bancadas de robótica/eletrônica e auditório. Lousas e identificação usam o assunto de cada sala. Redes, segurança e DevOps incluem racks; mobile e computação gráfica têm objetos de estudo próprios; robótica e embarcados têm instrumentos distintos. Cadeiras e estantes reutilizam os meshes compartilhados existentes.

### Quadro e easter eggs nos computadores

A fotografia original **“WhatsApp Image 2026-09-27 at 01.51.59.jpeg”** foi copiada sem alterar seus bytes para `Textures/Cowboy crew.jpeg`, dentro de `CampusBlock4`. O jogo referencia essa cópia; não depende do arquivo na Área de Trabalho. O quadro usa a imagem completa, com proporção 4:3, em frente ao bebedouro. A legenda atual tem somente duas linhas: **TURMA DO RODEIO** e **CIÊNCIAS DA COMPUTAÇÃO**.

A fotografia **`images.jpg`** enviada depois foi incorporada sem alterar seus bytes em `Textures/Secretariat portrait.jpg`. Ela ocupa um quadro de madeira na parede oeste interna da secretaria, acima da mesa secundária, próximo da estante. O material exclusivo é `Secretariat portrait photograph`; plano, moldura e fundo reutilizam meshes e materiais existentes. Nenhum novo collider foi acrescentado para essa decoração.

Quatro computadores têm telas especiais, cada uma mostrando uma pessoa diferente da foto, numeradas da esquerda para a direita: **402 → Cowboy Dev 01**, **407 → 02**, **414 → 03**, **420 → 04**. Os terminais incluem brincadeiras como `git rodeio`, `sudo cafe`, `npm run boi` e `BOI: MAIN`. Os rostos usam quatro meshes de UV com regiões distintas da **mesma textura original**, sem gerar cópias recortadas da imagem. Materiais exclusivos: `Cowboy crew photograph` e `Cowboy crew LCD portraits`. A emissão das telas é configurada no material salvo; não há script de easter egg em runtime.

Os antigos meshes e colliders da escada/elevador foram reaproveitados como segmentos adjacentes da parede sólida. A geometria antiga foi removida dos buffers, incluindo os antigos cabeçalhos; os assets, GUIDs e fileIDs foram preservados. A moldura reutiliza o cubo e o plano persistentes existentes do cenário.

### Portas, vidros e carregamento

As **41 portas internas** usam `OpenDoorWithKey`: 24 salas, secretaria, quatro entradas de banheiro e 12 cabines. Quarenta abrem por aproximação. A sala 423 exige ativação manual pelo teclado do enigma; todas permanecem abertas após destrancar. O trigger é fixo; somente a folha, seu collider e as maçanetas giram na dobradiça. Batentes, janelas e paredes são independentes da folha. `enableShaderReload = false` em todas as portas internas.

Os vidros são planos separados de quatro vértices, com collider fino próprio; não são cubos transparentes combinados às paredes. O material `Observation glass` usa alpha 0,055, fila 3000, `ZWrite = 0` e não projeta sombras. Os vãos das portas e janelas são aberturas reais na geometria. Paredes coincidentes entre salas adjacentes foram substituídas por uma única divisória. As juntas decorativas são superfícies simples, sem faces laterais e sem sombras próprias.

| Objeto | Posição do root/trigger | Orientação da chegada |
| --- | --- | --- |
| Exterior: trigger de entrada no Bloco 4 | `(63, 2.05, 30.55)` | — |
| Exterior: chegada `FromCampusBlock4` | `(63, 1.91, 29.65)` | Y = 180°; voltado para o campus |
| Interior: chegada `FromCampusExterior` e `SpawnPoint` | `(0, 1.11, -30.4)` | Y = 0°; voltado para o fundo |
| Interior: trigger de saída | `(0, 1.4, -33.55)` | — |

As chegadas ficam fora dos triggers de retorno. O portal externo está no prefab **e** no root serializado da cena exterior. O shell externo existente foi preservado. O carregamento continua pelo `DoorReloadScreen`, com rótulo **BLOCO 4** e recurso **`Textures/UI/Transitions/CampusBlock4`**, atualizado com a captura desta planta. Pause e checkpoint reconhecem `CampusBlock4`. Jogador, vida, munição e HUD continuam sob o `GameManager` existente. Somente a entrada/saída do prédio troca de cena.

### Enigma das fotografias — sala 423

As cinco fotografias têm placas azuis com **formas geométricas e números**, sem depender de cores. As placas dos quatro retratos ficam presas acima das telas; a quinta fica abaixo do retrato na secretaria. A ordem aparece em uma placa separada abaixo da legenda do quadro da turma, em frente ao bebedouro. A pedido do usuário, essa placa mostra **somente as cinco formas**, sem título, instrução ou setas. A legenda da turma permanece com as duas linhas originais.

| Local da pista | Símbolo | Número |
| --- | --- | --- |
| 402 — Programação I | Quadrado | 1 |
| 407 — Banco de Dados | Triângulo | 2 |
| 414 — Desenvolvimento Mobile | Círculo | 3 |
| 420 — Qualidade e Testes | Losango | 4 |
| Secretaria | Cruz | 5 |

**Solução para manutenção (spoiler):** triângulo → losango → quadrado → cruz → círculo, portanto **24153**. A configuração está em `CampusBlock4/Block4 photo puzzle.asset`; `Block4PuzzleDefinition` valida as cinco formas, números únicos 1–5 e sequência completa. A porta compara os dígitos com essa configuração. As placas e meshes são assets salvos; alterações futuras da configuração devem manter as placas físicas correspondentes.

A **sala 423 — Pesquisa e TCC** guarda a recompensa. Seu `OpenDoorWithKey` usa `requiresManualActivation = true`, sem alterar o comportamento das outras 40 portas. O terminal físico fica ao lado do batente, em `(11.65, 1.48, 14.04)`. Dentro de 2,4 m e com linha de visão livre até o terminal, **E** abre o teclado; no controle, o botão oeste (**X** no Xbox). O painel aceita mouse, dígitos superiores e teclado numérico; **Backspace/Delete** apagam, **Enter** confirma e **Esc** fecha. Há botões para apagar, limpar, confirmar e fechar. A entrada tem cinco dígitos; tentativas incompletas ou incorretas mantêm a porta trancada. Digitar após erro substitui a tentativa anterior.

`GameMenus.Interaction.cs` compartilha a propriedade do pause dos menus: movimento, câmera, armas, áudio e HUD são suspensos durante a digitação. O fechamento consome o frame de entrada para evitar disparos ou abertura do pause pelo mesmo clique/Esc. O painel respeita a área segura e o tamanho disponível. Morte, destruição do proprietário e saída da cena liberam a interação.

A chave fica em um pedestal dentro da 423, com trigger em `(12.9, 1.27, 20.35)`. O visual atual reutiliza **o mesmo sprite, material e escala do `Key03.prefab` da sala do diretor**, com `Billboard` e flutuação; não gira como um modelo 3D. O visual fica 0,30 m acima do trigger para não atravessar o pedestal. As peças antigas de latão estão inativas, preservando seus fileIDs e referências. `Block4TeacherKeyPickup` só coleta a chave com a senha resolvida, a porta totalmente aberta, o jogador vivo e o gameplay ativo. Recolhe uma única vez, toca o som existente e mostra a confirmação no HUD. **Nenhuma cela está ligada a essa chave ainda.** Ela não altera `Key01..03` nem `PrisonProgress.HasKey`.

O inventário da partida fica em `UniDoom.CampusBlock4.Block4PuzzleProgress`: `DoorUnlocked`, `HasTeacherCellKey` e o identificador `TeacherCellKeyId = "block4.teacher-cell"` estão disponíveis para a futura cela do professor. A porta destrancada e a chave coletada sobrevivem às transições e à recarga de checkpoint; a chave recolhida não reaparece. `GameManager.EndSession()` e o início do runtime limpam esse progresso para a próxima partida. Não há gravação desse estado em disco.

### Persistência e manutenção

O inventário exclusivo atual é de **963 meshes**, **10 materiais**, **1 perfil**, **1 configuração do enigma**, **4 scripts locais**, **1 prefab**, **1 cena**, **2 fotografias JPEG** e **1 PNG de carregamento**, mais `.meta`. A geometria e os materiais são persistentes; a interface do teclado é montada em runtime seguindo o padrão existente de `GameMenus`. Os nomes históricos dos meshes originais foram mantidos para preservar caminhos, GUIDs e referências, embora a geometria tenha sido atualizada para esta planta.

Foram preservados todos os arquivos e `.meta` existentes do Bloco 4, os **5.135 fileIDs** originais do prefab e os **21 fileIDs** originais da cena. Objetos antigos foram reaproveitados; colisores de divisórias duplicadas foram transformados em células adjacentes do piso, sem colisão sobreposta. Meshes novos intermediários sem dependência na cena ficaram fora da integração. Nenhum asset anterior do usuário foi excluído nesta revisão.

A paleta de 24 materiais em `Global/Materials/CampusInterior/`, `GraniteSpeckle.png` em `Global/Textures/CampusInterior/` e os sete meshes de cadeiras/estantes em `Global/Meshes/CampusInterior/` são compartilhados com a biblioteca. A migração dessas pastas pertence à implementação inicial; **esta revisão de planta não moveu nem modificou esses recursos**, a biblioteca ou a fachada exterior.

As ferramentas pontuais ficam na cópia isolada preexistente `.utmp/DirectorRoomValidation/Assets/Editor/`; saídas temporárias em `Temp/Block4Diagram/`, `Temp/Block4EntranceFix/`, `Temp/Block4Cowboys/`, `Temp/Block4SecretariatPortrait/` e `Temp/Block4Puzzle/`. Não foram adicionados geradores em `Assets` do projeto principal. Capturas selecionadas ficam em `output/CampusBlock4-*.png`.

### Validações anteriores da planta e decoração — 06/11/2026

Executadas na cópia isolada, com Unity **6000.5.7f1**, scripts e dependências atuais:

- Compilação; abertura do prefab e da cena; ausência de scripts, materiais ou shaders faltantes; meshes persistentes verificados. Meshes transitórios do TextMeshPro são esperados e usam as fontes salvas do projeto.
- **42 rotas completas** de NavMesh a partir da entrada, cobrindo as 24 salas, secretaria, bebedouro, aproximação do quadro, bancos/catracas e 12 cabines. NavMesh temporário calculado das colisões salvas, com raio 0,27 m, altura 2,15 m e voxel 6 cm; nenhum NavMesh de teste integrado.
- **63 amostras** ao longo do corredor principal sem obstrução e **72 amostras** de folga nos vãos das salas com folhas abertas.
- Play Mode automatizado com o `CharacterController` real: **45 percursos físicos** — corredor principal inteiro, os dois cruzamentos, 24 salas, secretaria, quatro banheiros, 12 cabines e aproximação do quadro. As **41 portas** abriram por aproximação; nenhuma passagem interna solicitou carregamento.
- Collider de vidro testado por raycast e tentativa física de atravessar a janela. Entrada pela fachada, arte de loading correta, chegada atualizada, saída e reentrada, sem loop de transição ou player duplicado. Vida, carregador e reserva preservados.
- Reabertura em nova sessão da Unity e inspeção das capturas de hall, ala com salas opostas, aula, informática, pesquisa, robótica, banheiro, secretaria, catracas e portas/vidros. O refinamento final das juntas decorativas altera somente renderização, sem mudar as colisões testadas.
- Comparação dos assets integrados com a cópia validada, preservação de GUIDs/fileIDs e resolução das referências. Os bytes de dependências compartilhadas permaneceram iguais. `git diff --check` executado após a integração.

Na revisão anterior da entrada e do fechamento da secretaria, antes da instalação do quadro, foram repetidos os testes das **42 rotas**, **45 percursos físicos**, **41 portas**, vidros e transições. Foram acrescentadas verificações da posição das quatro catracas e duas tentativas físicas de entrar atrás da secretaria, ambas bloqueadas. Todos os **11.865 fileIDs** do prefab e os **21 fileIDs** da cena existentes antes daquela correção foram preservados.

Na instalação do quadro e das telas de cowboy, os **11.892 fileIDs** existentes do prefab e os **21 fileIDs** da cena foram preservados. A foto foi comparada por SHA-256 com o JPEG original. Abertura da cena/prefab, compilação, as **42 rotas**, **45 percursos físicos**, **41 portas**, colisão dos vidros e ida/volta ao campus foram verificados novamente. O jogador tentou entrar no antigo acesso técnico e nas duas brechas da secretaria: os três acessos ficaram bloqueados. Os quatro monitores foram encontrados em Play Mode; UVs distintos e textura compartilhada foram conferidos. A emissão das telas foi refinada depois do teste físico, sem alterar colisões. Capturas na Unity verificam a foto completa, os quatro retratos e a nova parede. Prévias: `CampusBlock4-Quadro-Cowboys.png` e `CampusBlock4-Cowboy-Dev-01.png` até `04.png`. Os assets integrados foram comparados com a cópia isolada, incluindo os `.meta` e a resolução de GUIDs; não houve modificação de dependências compartilhadas.

Como no teste anterior, o log do Play Mode registrou uma exceção de indexação em `UnityEditor.Search.SearchDatabase` durante a inicialização do Editor; as verificações automatizadas de gameplay concluíram com sucesso. A origem desse aviso de Editor não foi investigada nesta tarefa.

Na alteração decorativa do retrato da secretaria e da legenda, foram verificados compilação, abertura do prefab/cena, materiais, scripts, 41 portas e capturas da secretaria e do quadro da turma. Todas as colisões existentes foram comparadas antes/depois e permaneceram idênticas, inclusive após salvar e reabrir a cena. Foram preservados os fileIDs e `.meta` anteriores, e a nova foto foi comparada com o original por SHA-256. Não foi repetido o Play Mode nem gerado build nessa alteração decorativa. Prévias: `CampusBlock4-Secretaria-Retrato.png`, `CampusBlock4-Secretaria-Interior.png` e `CampusBlock4-Quadro-Cowboys.png`.

### Validação do enigma — 06/11/2026

Executada na cópia isolada com Unity **6000.5.7f1**, sem interferir no Editor principal:

- Compilação, prefab/cena salvos e reabertos; configuração, materiais, shaders, scripts e meshes persistentes conferidos. Preservados os **12.005 fileIDs** anteriores do prefab e os **21** da cena, todos os `.meta` anteriores e as fotografias originais. Os **1.570 registros de colliders/rigidbodies** anteriores permanecem idênticos; os **3.405 transforms** anteriores mantêm posição, rotação, escala e pai.
- Play Mode com o jogador real: porta bloqueia fisicamente antes da senha; uso remoto do terminal recusado; chave não pode ser coletada antes do enigma, inclusive no teste de teleporte para dentro da sala.
- E, dígitos superiores, numpad, Backspace, Enter, Esc e botões da interface testados; limite de cinco dígitos, erro, tentativa incompleta, apagar/limpar, reabertura, bloqueio de opções, fechamento e limpeza de morte. Movimento/câmera suspensos, disparos bloqueados e nenhum tiro no fechamento. O foco inicial do controle está implementado; não houve sessão manual com controle físico.
- Senha correta, feedback, animação real da dobradiça, travessia com `CharacterController`, coleta por aproximação e inventário independente verificados.
- **45 percursos físicos**: corredor principal, dois cruzamentos, 24 salas, secretaria, quatro banheiros, 12 cabines e aproximação do quadro. As 40 portas livres e a 423 após a senha ficaram abertas. Vidros, catracas, parede do quadro e fechamento atrás da secretaria novamente conferidos. Nenhuma passagem interna carregou outra cena.
- Entrada e loading do exterior, saída/reentrada, um único jogador e conservação de vida/munição verificados. Reentrada e **recarga real de checkpoint** conservaram a porta e a chave; uma **nova partida** restaurou a porta trancada e a recompensa disponível.
- Capturas do teclado em **1280×720, 1920×1080 e 1024×768**, sem overflow dos textos ou recorte do painel; revisão visual das cinco pistas, ordem, porta, feedback e confirmação de coleta. Prévias em `output/CampusBlock4-Enigma-*.png`.
- Integração comparada por bytes com a cópia validada, resolução de GUIDs e preservação dos assets anteriores. Nenhum gerador de autoria foi integrado a `Assets` do projeto principal.

O aviso preexistente de `UnityEditor.Search.SearchDatabase` voltou a aparecer na inicialização do Editor isolado; os testes de gameplay concluíram com sucesso. O NavMesh não foi recalculado nesta implementação; as rotas desta revisão foram percorridas fisicamente no Play Mode.

### Refinamento dos símbolos, chave e quadro da biblioteca — 06/11/2026

Na cópia isolada da Unity, foram executadas compilação, abertura dos dois prefabs/cenas e comparação visual das capturas. A placa abaixo do quadro mostra apenas cinco símbolos; a senha e as cinco pistas permanecem iguais. A nova chave usa exatamente os recursos visuais da chave da sala do diretor. O retrato da recepção utiliza a fotografia original, sem recorte ou mudança de proporção.

O Play Mode conferiu a orientação do billboard para a câmera real, a folga acima do pedestal, desbloqueio pelo código existente, travessia física e coleta única da chave. Também percorreu Bloco 4 → exterior → `CampusLibrary` → exterior, verificando o quadro, a visão desobstruída a partir do hall, um único jogador e a conservação de vida, munição e inventário. Todas as colisões anteriores dos dois prefabs foram comparadas e permaneceram idênticas; os fileIDs e `.meta` anteriores foram preservados. Os bytes da foto foram comparados com `Screenshot_1.png`.

Ferramentas pontuais na cópia isolada; registros temporários em `Temp/CampusPortraitPolish/`. Nenhum gerador foi integrado aos assets finais. Prévias adicionais: `output/CampusBlock4-Enigma-Quadro-Sem-Dica.png` e `CampusBlock4-Enigma-Chave-Padrao.png`. Não foram repetidas as 45 rotas nem o NavMesh neste refinamento visual; não houve build ou medição com Profiler. O aviso preexistente de indexação de `UnityEditor.Search.SearchDatabase` reapareceu durante a inicialização do Editor isolado, sem impedir a conclusão do teste.

**Não foi gerado executável nem realizado benchmark com Profiler.** O Play Mode foi automatizado na cópia isolada, sem uma sessão manual completa no Editor principal. A validação não constitui uma medição de FPS.

## 13. Integração com development — 06/11/2026

Merge de `origin/development` (`688a7298`) na `feat/bloco4` (`a6d2672e`). Os conflitos envolveram a cena do exterior, seu prefab e este documento. As duas branches adicionaram objetos nas mesmas regiões do YAML; a resolução preservou todos os objetos recebidos e acrescentou os dois portais/chegadas do Bloco 4, mantendo os fileIDs e referências. A cena passou de 4.384 para 4.391 registros e o prefab de 4.281 para 4.288; a alteração nos roots foi somente a união das listas de filhos. Permanecem as passagens para porão, biblioteca, Direito e Bloco 4, além das novidades de paisagismo/estacionamento da development. As onze cenas continuam habilitadas.

Validação desta integração:

- Conferência estática dos 17.790 caminhos rastreados: arquivos presentes, nenhum GUID duplicado e nenhum ponteiro LFS pendente entre os formatos binários conferidos. Cena e prefab do exterior sem IDs locais duplicados, referências locais ausentes ou marcadores de conflito. Os arquivos recebidos não foram substituídos pela versão inteira de uma das branches.
- Unity **6000.5.7f1**, na cópia isolada: compilação, abertura do prefab do exterior e das cenas `CampusExterior`, `CampusBlock4`, `CampusLibrary` e `LawSchool`; scripts, materiais e shaders resolvidos. Foram verificados os quatro destinos, suas chegadas, a senha/41 portas do Bloco 4, a placa sem dica, a chave com sprite, o retrato da biblioteca e o guard da saída de Direito.
- Play Mode: resolução do enigma, travessia física e coleta da chave; Bloco 4 → exterior → biblioteca → exterior → Direito → exterior. Um único jogador, vida, munição e chave do Bloco 4 foram preservados. A saída de Direito foi testada antes de ativar o alarme; o ciclo completo desse alarme não foi repetido nesta integração.
- As 4.303 dependências enumeradas na validação foram comparadas entre o projeto principal e a cópia isolada, sem diferenças de conteúdo. O cache isolado tinha registros de GUID antigos para onze assets; esses registros foram reconstruídos preservando os bytes dos arquivos e `.meta`. Isso não exigiu alterar os modelos ou materiais originais do jogo.

O aviso preexistente de indexação de `UnityEditor.Search.SearchDatabase` reapareceu durante a inicialização do Play Mode, sem impedir a conclusão dos testes. Não houve nova comparação visual detalhada, recálculo de NavMesh, repetição das 45 rotas, build ou benchmark nesta integração. Ferramentas e registros temporários ficam fora dos assets finais, em `Temp/CampusMerge/` e na cópia isolada de validação.
