# Campus Exterior e Biblioteca do Campus — contexto de implementação

> Documento para continuar o trabalho sem reconstruir o contexto da conversa nem ler milhares de assets. Atualizado em **05/11/2026**, com os arquivos presentes e o histórico desta sessão. Base versionada: `development`, commit `494fdd18`. Alterações futuras podem mudar os números e parâmetros abaixo.

## 1. Leitura rápida para a próxima IA

- **Campus Exterior** é o cenário com gramados, árvores, avenidas, portaria UNIMAR, fachadas e caixa-d'água: cena **`CampusExterior`**.
- A nova biblioteca desse campus tem interior separado: cena **`CampusLibrary`**. Entrar e sair usa a transição existente do jogo, ao tocar no volume da porta.
- **`Library` é a biblioteca antiga do começo do jogo. Não foi substituída pela nova biblioteca.** Não confundir também `CampusExterior` com `EntranceGarden` ou `Main/SampleScene`.
- O exterior e o interior já estão salvos em cenas, prefabs, meshes e materiais persistentes. **Não precisam ser gerados novamente para jogar.**
- O exterior possui **12 vagas de estacionamento** demarcadas em três trechos viários, **12 bancos de concreto** nos três pátios sociais e uma **fonte ornamental exclusiva** no pátio entre o Bloco de Direito e a biblioteca, em `(-58, 0, 16)`.
- A revisão mais recente da biblioteca corrigiu móveis apertados, peças flutuando, suportes, portas dos banheiros e circulação. Preserve essas correções.
- O jogador é persistente entre cenas pelo `GameManager`; não crie outro sistema de player, HUD, inventário ou carregamento para esses ambientes.
- Antes de alterar, leia a seção correspondente abaixo e somente os scripts/assets envolvidos. Não despeje cenas, prefabs e arrays de vértices inteiros no contexto.
- Respeite o [AGENTS.md](AGENTS.md). Preserve alterações locais, GUIDs, fileIDs e `.meta`. Exclusões definitivas exigem autorização explícita.
- Houve um incidente em que `Assets` ficou vazia. Os **8.908 arquivos rastreados foram recuperados**, sem faltar arquivo; **504 assets LFS** foram verificados por SHA-256. **A causa do desaparecimento permanece desconhecida.** Veja a seção 10 antes de interpretar o status do Git.

## 2. Mapa do projeto e identificação correta dos cenários

| Cena | Caminho em `Assets/_Project/Scenes/` | Papel |
| --- | --- | --- |
| `CampusExterior` | `CampusExterior/CampusExterior.unity` | Campus externo com grama, avenidas, árvores e edifícios desta implementação. |
| `CampusLibrary` | `CampusLibrary/CampusLibrary.unity` | Novo interior da biblioteca ligada ao Campus Exterior. |
| `Library` | `Library/Library.unity` | Biblioteca anterior, preservada. |
| `EntranceGarden` | `EntranceGarden/EntranceGarden.unity` | Jardim/entrada anterior; não é o novo Campus Exterior. |
| `SampleScene` | `Main/SampleScene.unity` | Cenário anterior. O menu ainda o apresenta como **CAMPUS COMPLETO**; esse texto não identifica `CampusExterior`. |
| `Basement` | `Basement/Basement.unity` | Porão/prisão; possui a passagem para o Campus Exterior. |
| `Cylinder`, `ClassRoom`, `DirectorRoom` | Pastas de mesmo nome | Ambientes anteriores; o padrão de transição foi reaproveitado. |

**Ambiente registrado:** Unity **6000.5.7f1**, URP **17.5.0**, Cinemachine **3.1.7**, Input System **1.20.0**, AI Navigation **2.0.14**. As fontes de verdade são `ProjectSettings/ProjectVersion.txt` e `Packages/manifest.json`.

As dez cenas estão habilitadas em `ProjectSettings/EditorBuildSettings.asset`. Após integrar o Bloco de Direito e a biblioteca, `CampusExterior` é a oitava entrada, `LawSchool` a nona e `CampusLibrary` a décima. Prefira os nomes das cenas a índices fixos. O contexto do Bloco de Direito e sua ligação com o exterior está em [Docs/Maps/LawSchool.md](Docs/Maps/LawSchool.md#conexões).

## 3. Onde editar cada coisa

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
| Geometria e acabamento da nova biblioteca | `Assets/_Project/Scenes/CampusLibrary/Meshes/`, `Materials/` e `Textures/` |
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

Inventário exclusivo atual, sem `.meta`: **178 meshes**, **34 materiais**, **3 modelos FBX**, **6 prefabs**, **23 PNGs**, **1 HDR**, **2 scripts**, **1 shader**, **1 perfil** e **1 cena**. Esses números não incluem recursos compartilhados ou conteúdo interno dos modelos importados; não são uma medida de draw calls.

### Estacionamento e mobiliário dos pátios

- `Street Parking Bays - 12` usa um mesh persistente com quatro vagas na avenida oeste, quatro na via norte e quatro na via sul. As linhas ficam ligeiramente acima do asfalto para evitar z-fighting e não possuem colisão própria.
- `CampusConcreteBench` reproduz o banco de referência com assento e encosto de concreto claro, dois pedestais escuros e pés largos. Cada um possui um `BoxCollider`; os três pátios sociais instanciam quatro bancos, totalizando **12**.
- A fonte ornamental fica somente no `West social courtyard`, entre o Bloco de Direito e a biblioteca. A fonte simples que ocupava o mesmo ponto foi desativada apenas nessa instância, evitando sobreposição; as fontes simples dos outros pátios foram preservadas.
- `CampusOrnateFountain` possui piscina larga, dois níveis, coluna, figura central, quatro esculturas estilizadas e nove jatos visuais. A instância salva está em `(-58, 0, 16)` e possui colisão na bacia inferior.
- A ferramenta identificada em `Assets/_TemporaryCampusExterior/Editor/CampusParkingBenchFountainSetup.cs` aplica e valida esses assets. Preserve-a enquanto ainda houver necessidade de regeneração ou revisão; não trate o script como gameplay.

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
```

| Origem | `destinationScene` | `destinationSpawnId` | Ponto de chegada na cena de destino |
| --- | --- | --- | --- |
| Saída da prisão, no prefab `Basement` | `CampusExterior` | `FromPrison` | `Arrival from prison` |
| `Return to prison`, no exterior | `Basement` | `FromCampus` | `SceneArrivalPoint` com ID `FromCampus`, no prefab `Basement` |
| `Library entrance - CampusLibrary`, no exterior | `CampusLibrary` | `FromCampusExterior` | `Arrival - FromCampusExterior` |
| `Exit to Campus Exterior`, na biblioteca | `CampusExterior` | `FromCampusLibrary` | `Arrival - FromCampusLibrary` |

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

- Planta de aproximadamente **56 × 24 m**, com entrada pela frente em `z` negativo.
- Hall de entrada, atendimento e espera no patamar superior, a **0,64 m** do nível inferior.
- **Quatro degraus de 0,16 m**, com rota tátil azul e corrimãos, conectando hall e leitura/acervo.
- Recepção com balcão, trecho rebaixado, computador de atendimento e quadro de avisos.
- Área de leitura e estudo em grupo à esquerda, com mesas retangulares e redondas.
- **Seis posições individuais de estudo** no patamar de entrada.
- **Oito computadores de pesquisa**, mais um de atendimento: **nove gabinetes** no total.
- **32 estantes completas** e **51 cadeiras de leitura** no prefab atual.
- Dois banheiros, com **seis cabines no total**, bancadas, cubas, divisórias e acessórios.
- Piso de granito, teto modular, vigas alaranjadas, pilares, luminárias, sinalização, plantas, extintores, lixeiras e detalhes de uso universitário.

A geometria persistente está no prefab e nos meshes; a biblioteca não possui um gerador de layout necessário em runtime. O inventário exclusivo atual é de **252 meshes**, **24 materiais**, **1 perfil**, **1 prefab**, **1 cena** e **2 PNGs**, além dos `.meta`.

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

