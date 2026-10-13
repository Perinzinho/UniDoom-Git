# Porão e sala de aula — acabamento da sala do diretor

A revisão usa os materiais de `Assets/Art/DirectorOffice` como referência.
Os materiais próprios do Basement e ClassRoom recebem cópias dos acabamentos,
preservando seus GUIDs: reboco, piso, teto, madeira, metal, papel e luz quente.
O porão mantém ferrugem e sinais vermelhos de combate. A sala de aula mantém
seu quadro, mapa, janelas quebradas, obstáculos e entulho; suas texturas próprias
agora usam filtro Point, limite de 128 px e importação sem compressão.
Os materiais e a geometria da sala do diretor permanecem como referência.

A arena passa de 26 × 30 m para 34 × 30 m no prefab. O teto sobe de 4 para 6 m,
com 5,5 m entre piso e teto. Os pilares ficam a ±8,5 m do centro; caldeiras,
armários e plataformas acompanham as novas paredes mantendo suas dimensões.
A câmara final passa de 18 × 18 m para 22 × 18 m e seu teto sobe para 6,2 m.
Na instância atual do mapa, com escala não uniforme, a arena tem aproximadamente
20,17 m de largura e 5,30 m livres até o teto.

Entradas, saída, corredor em L, dimensões das folhas das portas e escala do player
são preservados. Os 12 pontos de inimigos e o volume do encontro acompanham
a arena ampliada. O portão ainda bloqueia o acesso até a última morte.

`RoomStyleBuilder.cs` roda somente na cópia isolada `.utmp/BasementPreview`,
junto com `BasementBuilder.cs`, `BasementPassageBuilder.cs` e `ClassRoomBuilder.cs`.
`Build` aplica esta revisão sobre os snapshots anteriores à ampliação.
`RefreshStyleAndVerify` reaplica os materiais e repete a validação sem ampliar novamente.
`Verify` verifica a revisão já instalada. Os dois primeiros métodos devem ser
executados após a geração das salas para manter o acabamento compartilhado.

`install_room_style.py` usa `.utmp/RoomStyle-before` para preservar blocos YAML,
IDs, dados ProBuilder e referências de malhas. Sem argumentos, grava a versão
preservada na cópia isolada para validação. `--install` instala no projeto principal
após verificar que os arquivos e materiais não receberam mudanças concorrentes.
Na SampleScene, somente os overrides afetados da instância do Basement são
copiados; os outros blocos permanecem idênticos ao snapshot do início da revisão.

Validação no editor: passagem de ida e volta com o CharacterController real no
prefab e no mapa, bloqueio do portão, encontro com 12 inimigos, dimensões livres
da arena e verificações existentes de circulação e abertura da porta da sala de aula.
As prévias estão em `Tools/Basement` e `Tools/ClassRoom`; as cenas de inspeção
estão nas pastas de arte correspondentes. A simulação não substitui playtest da IA.

## Comparação com o cilindro

`ArtStyleAudit.AlignAndCompare` atualiza as cópias privadas do cilindro para os
acabamentos atuais do porão e da sala de aula, ambos derivados da sala do diretor.
Também uniformiza o tom das luzes normais para o quente da sala do diretor e a
importação das texturas próprias do cilindro para Point e limite de 128 px.
As três portas funcionais continuam com iluminação vermelha exclusiva.
O mosaico, concreto rachado e decalques do cilindro permanecem distintos.

O audit verifica shader, textura, cor, brilho e metal dos acabamentos copiados,
confere a amostragem e a resolução de todas as texturas visíveis, e renderiza
os quatro ambientes sob o mesmo ambiente e luz de inspeção, sem pós-processamento.
`ArtStyleAudit.Compare` repete apenas a conferência e os renders.
As imagens e a tabela de materiais estão em [Comparison](Comparison/Comparison.md),
com uma [comparação lado a lado](Comparison/comparison.html).

`install_art_style.py` usa os snapshots `.utmp/ArtStyle-before` e os resultados
da cópia isolada. Instala os materiais e a importação do cilindro; no prefab,
preserva todos os blocos de geometria e copia somente as cores das luzes locais.
Não altera a cena principal nem o ambiente global do jogo.
