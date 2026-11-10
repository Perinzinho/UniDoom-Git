# CampusLibrary — missão do gerador

Implementação de 09/11/2026 baseada no README fornecido pelo usuário, no projeto existente, Unity 6000.5.7f1 / URP 17.5.0. A biblioteca antiga `Scenes/Library` é independente.

## Cenário persistente

Cena: `Assets/_Project/Scenes/CampusLibrary/CampusLibrary.unity`, instância de `Prefabs/CampusLibrary.prefab`. A ala **Emergency mission - annex and maintenance** está salva nesse prefab, sem geração de layout em runtime e sem overrides manuais na cena.

- Acréscimo ao norte de aproximadamente **790 m²**, cerca de **33%** sobre a planta anterior, até `z ≈ 44,3`. Envelope nominal aproximado **56 × 56,5 m**; área bruta estimada, sem descontar móveis/paredes.
- Passagem de **4 m**, centrada em `x = 18,5`, na parede de `z = 30,3`, altura livre **3,25 m**. Recorte de dois meshes exclusivos e adaptação do collider da parede, com identidades preservadas.
- **12 estantes**, **6 mesas**, **24 cadeiras**, livros, notas e luminárias adicionais. Totais de estantes/cadeiras: **74 / 119**. Tampos a 0,96 m, com circulação entre os grupos.
- Manutenção interna em aproximadamente `x = -27…-17`, `z = 36…44,3`. Acesso de **2,20 × 3,20 m**, em `(-21,5; 0; 36)`, gerador, sete terminais, três encaixes e esquema de partida.
- Entrada externa, chegada `FromCampusExterior`, saída, degraus, sanitários, foto, chaves numeradas e mobiliário anterior preservados. Sem segundo andar ou escada de subsolo.
- Materiais, cube mesh persistente, estantes, cadeiras, fontes e sons reutilizados. Nenhum pacote, download ou mídia externa acrescentado; fontes/licenças anteriores de CampusLibrary continuam aplicáveis.

## Progressão e controles

Olhar e usar **E** ou **X do controle**, com alcance de 2,8 m, raycast, obstáculo, jogador vivo, gameplay ativo e bloqueio de entradas repetidas. O painel permanece no mundo; não captura cursor/movimento ou abre um menu.

1. `NotDiscovered`: três fusíveis inteiramente inativos. Luz normal desligada, vermelhas com pulsação suave de 0,55 Hz e ambiente mínimo estável.
2. Proximidade do gerador: `SearchingFuses`, toast inferior e ativação de exatamente três IDs **0, 1 e 2**. Reaproximar-se não reinicia nada.
3. Cada coleta registra seu bit exclusivo e desativa somente aquele item; IDs inválidos/repetidos são recusados. Terceiro item: `FusesCollected`, sem instalação automática.
4. Voltar ao comando frontal e usar **Instalar fusíveis**: `FusesInstalled`, três indicadores preenchidos e botões liberados.
5. Sequência inicial **3 → 1 → 5 → 2 → 7 → 4 → 6**, em sete botões físicos numerados. Erro reinicia somente a tentativa. Segmentos verdes mostram avanço. Solução travada em `PuzzleSolved`.
6. Usar **Ligar gerador** no comando frontal: `GeneratorOn`, vermelhas apagadas, luz/ambiente normais restaurados e reforços encerrados. Resolver o painel não aciona o gerador sozinho.

Campo `sequence` de `LibraryMissionController`: sete números distintos de 1 a 7. A pista física e a leitura contextual acompanham esse campo. O esquema está na parede leste da manutenção; E também mostra a ordem no toast existente.

| ID | Local | Pivô do fusível |
|---|---|---|
| 0 | Carrinho de apoio do novo acervo | `(9,4; 0,825; 34,5)` |
| 1 | Mesa da nova área de estudo | `(-11; 1,025; 37,3)` |
| 2 | Balcão existente da recepção | `(-6; 2,09; -8,25)` |

Destaque luminoso moderado acompanha o coletável, inativo antes da descoberta. Sons existentes de coleta, acionamento mecânico, botão e erro. Sem inventário global novo.

## Persistência e integração futura

`UniDoom.CampusLibrary.LibraryMissionProgress` guarda estágio, máscara de IDs e progresso do circuito durante a partida, seguindo o padrão de `Block4PuzzleProgress`. Saída/reentrada e checkpoint preservam os dados. Não é save em disco; fechar o jogo ou iniciar outra partida limpa o progresso.

- Flag: `LibraryMissionProgress.IsLibraryGeneratorPowered`.
- Evento: `LibraryMissionProgress.OnLibraryGeneratorPowered`, uma vez na transição final. Recarregar a cena concluída não reemite o evento; consumidores tardios consultam a flag.
- `GameManager.EndSession()` e a inicialização do runtime limpam o estado.
- Assinantes devem se desinscrever ao serem destruídos/desativados.

**Nenhuma abertura, animação ou condição de cela foi implementada.** Chaves e sistemas prisionais anteriores não foram alterados.

## Zumbis e sala segura

`CampusLibraryZombie.prefab` é variante do `Global/Prefabs/Enemy/Enemy.prefab`: reutiliza sprite padrão, animações, saúde, dano, morte e loot, acrescentando agente NavMesh e restrição local. Aparências alternativas desabilitadas somente nessa variante mantêm a cápsula normal.

Entrada dentro da porta, `(0; 1,64; -8,5)`. Padrão **1 vivo**, configurável entre 1 e 2; reposição **4 segundos** após morte. Usa `EnemySpawnSafety`, mínimo **8 m**, câmera/visibilidade e NavMesh. Ponto inseguro aguarda; mortes repetidas não abrem slots extras. Corpos desses reforços removidos após 18 s. Sobreviventes continuam ativos após a energia; novos reforços param.

Proteção da manutenção:

1. `NavMeshModifierVolume` não navegável e NavMesh persistente em `Navigation/Library mission navigation.asset`.
2. Collider físico da entrada ignorado somente pela cápsula do jogador persistente, permitindo ida/volta.
3. `LibraryEnemyRestriction` recusa alvos protegidos no teste existente de visão/impacto e recupera atores colocados indevidamente dentro da sala, inclusive com agente desabilitado. Verifica na física e antes de renderizar; o volume inclui margem diante do batente.

`Enemy.cs` recebeu somente a consulta opcional desse componente. Inimigos sem a restrição mantêm o comportamento anterior.

## Arquivos e Inspector

Scripts em `Assets/_Project/Scenes/CampusLibrary/Scripts/`:

| Script | Responsabilidade |
|---|---|
| `LibraryMissionProgress.cs` | Estados, IDs, sequência, flag e eventos da sessão |
| `LibraryMissionController.cs` | Descoberta, interações, mensagens, indicadores e pista |
| `LibraryMissionTarget.cs` | Identificação de fusíveis, botões, comando e esquema |
| `LibraryLightingController.cs` | Emergência e restauração de luzes/ambiente |
| `LibraryMaintenanceSafety.cs` | Volume protegido e colisão seletiva |
| `LibraryEnemyRestriction.cs` | Proteção do alvo e recuperação do inimigo |
| `LibraryZombieSpawner.cs` | Limite, entrada segura, morte e reposição |

Alterações compartilhadas: reset em `GameManager.cs`, consulta opcional em `Enemy.cs` e API de mensagens no toast de `DeathScreen.cs`. O toast conserva fonte/animação, amplia texto de missão, limita repetições/fila e limpa essas mensagens ao sair. Checkpoint conserva seu tamanho/texto próprios.

Além do prefab principal e dois meshes do recorte: variante de zumbi, material vermelho local e NavMesh persistente, com `.meta`. **Não há referências para ligar manualmente:** gerador, descoberta, fusíveis, botões, LEDs, texto, áudio, luzes, entrada e área segura já serializados.

Ferramentas/logs temporários em `Temp/LibraryMission/` e na cópia isolada preexistente `.utmp/DirectorRoomValidation/Assets/Editor/`. Não são dependências do jogo. Nenhum gerador novo integrado aos assets finais.

## Validação desta implementação

Executada na Unity **6000.5.7f1**, na cópia isolada sincronizada com os arquivos atuais, usando player/cena reais:

- Compilação, abertura de prefab/cena e resolução de scripts, materiais e shaders.
- `CharacterController` pela passagem, corredor da ala e entrada da manutenção. Rotas do NavMesh persistente até a ala e aproximações das mesas.
- Fusíveis inativos, descoberta por proximidade, IDs únicos, coleta, duplicação recusada, reentrada com 1/3 e **recarga real de checkpoint com 2/3**.
- Painel bloqueado sem instalação, instalação explícita, erro sem perda de inventário, sete passos, entradas repetidas recusadas, solução travada e ignição separada.
- Vermelho antes, normal depois, reentrada concluída e evento emitido uma vez.
- Perseguição até entrada aberta da manutenção, jogador sem dano através do limite e recuperação de zumbi colocado à força dentro dela, inclusive sem agente.
- Um vivo, uma vaga por morte, reposição com intervalo, parada após conclusão e spawn junto da chegada recusado.
- Ida/volta por `DoorReloadScreen` entre biblioteca/campus, um jogador e munição preservados. Carregamento de `CampusBlock4` e `LawSchool`; teclado do Bloco 4 presente com estado independente.
- `GameManager.EndSession()` limpa a missão. Capturas da ala, manutenção e painel antes/depois revisadas visualmente.
- Teste complementar com teclado sintético do Input System confirmou **E → raycast → coleta**, com o foco do harness ajustado para execução em background. Input de missão recusado durante pausa. Pista física/contextual acompanha alteração da sequência no Inspector e o toast comporta a pista inteira sem overflow.
- Integração conferida por bytes com a cópia compilada/testada: **6.664 fileIDs anteriores preservados**, **8.319 registros finais**, `.meta` anteriores idênticos, referências locais/novos GUIDs resolvidos e nenhum GUID duplicado no projeto. Cena e assets compartilhados visuais permanecem intactos. `git diff --check` limpo após normalizar espaços finais das novas linhas emitidas pela Unity.
- Reabertura dos arquivos finais na Unity confirmou serialização, textos e referências. Comparadas **731 dependências da cena**, com arquivos e `.meta` iguais entre projeto principal e cópia validada. Dois fingerprints `userData` de silhuetas foram normalizados somente na cópia de teste; recursos/importadores do projeto principal preservados.

Prévias: [ala em emergência](../../output/CampusLibrary-Gerador-Ala-Emergencia.png), [ala com energia](../../output/CampusLibrary-Gerador-Ala-Energia.png), [manutenção em emergência](../../output/CampusLibrary-Gerador-Emergencia.png), [gerador ligado](../../output/CampusLibrary-Gerador-Energia.png) e [pista](../../output/CampusLibrary-Gerador-Pista.png).

O Editor isolado voltou a registrar a exceção **preexistente** de indexação `UnityEditor.Search.SearchDatabase`, sem impedir os testes. Não foi atribuída à missão; os componentes da missão passaram sem erros.

Não houve build, benchmark/Profiler, sessão manual completa ou teste com controle físico. Puzzles completos do Bloco 4 e missão inteira de Direito não foram repetidos: a regressão foi de carregamento/estado/continuidade do jogador. Suas cenas/prefabs não foram alterados.

## Teste manual de ponta a ponta

Começar partida nova e entrar pelo campus. Conferir ausência dos fusíveis. Ir ao fundo leste do acervo norte, cruzar **Acervo complementar** e seguir à esquerda para **Manutenção**. Descobrir o gerador, coletar os três itens da tabela e voltar para instalar. Tentar botão errado, ler o esquema e executar a ordem correta; só então acionar o gerador. Sair/voltar para conferir persistência. Durante a busca, eliminar o reforço e observar a reposição segura; atraí-lo até a manutenção para verificar o bloqueio.

## Integração com development — 10/11/2026

Branch local atualizada de `7e4bee86` para `455826b0`, preservando a implementação local do gerador. As duas chaves/cadeados de acesso recebidos do remoto continuam exigidos antes da entrada; a missão do gerador ocorre depois, dentro da biblioteca. Nenhuma abertura de cela foi acrescentada.

Não houve conflito de arquivo; os dois documentos sobrepostos foram combinados e a descrição de gerador futuro foi alinhada à missão existente. Os arquivos locais de código/assets foram preservados, com apenas normalização de finais de linha pelo Git. Oito imagens LFS recebidas foram baixadas e conferidas pelo SHA-256 registrado no commit.

Compilação C# isolada dos **123 scripts de runtime** e **14 de Editor** passou sem erros, usando referências/compilador da Unity instalada e saídas somente em `Temp`. Runtime com avisos existentes de API obsoleta e um campo não utilizado; Editor sem avisos. Não foi iniciada Unity, nem repetidos Play Mode, percursos, build ou comparação visual nesta atualização. Os testes de gameplay anteriores desta ficha permanecem históricos.
