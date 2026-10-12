# Porão — arena e câmara final

O `Assets/prefabs/Scenario/Basement.prefab` recebe modelos 3D com texturas pixeladas
128 × 128, filtro Point e materiais URP. A paleta acompanha as outras salas:
pedra suja, tijolos escuros, ferro, ferrugem, luz âmbar e detalhes vermelhos.

A arena contém caldeiras rebitadas, tambores, painéis elétricos com terminais,
tubulações com flanges, reforços de pilares, vigas, luminárias gradeadas,
drenagem e placas. A sala final tem altar, torres de energia, selo infernal,
anel ritual no piso e iluminação vermelha. Peças pequenas são agrupadas por
material em malhas persistentes; os equipamentos grandes têm colisões simples.

## Combate

`PreBossArea/BasementArenaDetails` contém `BasementArenaEncounter`.
Quando o objeto com tag `Player` entra na arena, são instanciados 12 inimigos
usando o prefab atual `Assets/prefabs/Enemy/Enemy.prefab`. Inimigos vivos já
presentes dentro da arena também entram na contagem. O corredor permanece
bloqueado até todos morrerem; a porta sobe e o indicador fica verde.

Os pontos estão em `Arena enemy spawn points`. Ajuste o array `spawnPoints`
e o `enemyPrefab` no Inspector para balancear a luta. Spawners de ondas futuras
devem chamar `RegisterEnemy` antes da arena ser limpa. Cada instância do porão
mantém sua própria lista. O encontro é único por carregamento da cena.

`BasementGateLayout` ajusta a porta ao piso, às laterais e ao cabeçalho originais,
incluindo os overrides da instância na cena. O collider acompanha a folha.

O ponto do boss fica em `BossRoom/BasementBossDetails/FinalBossSpawn`.
Posicione ali o prefab do boss final. A decoração não cria um boss nem implementa
suas habilidades, e a morte dele não faz parte da trava da primeira arena.

## Arquivos e geração

- Arte: `Assets/Art/Basement/{Textures,Materials,Meshes}`.
- Cena de inspeção: `Assets/Art/Basement/BasementPreview.unity`.
- Componentes: `Assets/Scripts/Scenario/BasementArenaEncounter.cs` e `BasementGateLayout.cs`.
- Fonte do gerador: `Tools/Basement/BasementBuilder.cs`, fora de `Assets`.
- Prévias: `Tools/Basement/Basement-*.png`, incluindo a instância do mapa.

Para gerar novamente, use uma cópia isolada do projeto **com o prefab original**,
copie o gerador para `Assets/Editor` e execute no Unity 6000.5.7f1:

```
-batchmode -quit -executeMethod BasementBuilder.Build
```

O instalador espera essa cópia em `.utmp/BasementPreview` e o original em
`.utmp/Basement-original.prefab`. Execute `python Tools/Basement/install_basement.py`
para conferir e preservar os blocos originais nessa cópia. `--install` transfere
os assets e o prefab para o projeto principal. Ele recusa alterações inesperadas.

Na instalação inicial da decoração, transformações, dados de ProBuilder e formas
de colisão da estrutura original foram preservados. Referências de malhas ausentes no prefab são preenchidas com
assets persistentes. Na `SampleScene`, apenas três instâncias de portas do porão
recebem ajustes pontuais: `DoorPrefab (5)` é desativada porque a nova porta de
combate assume sua abertura; `(4)` e `(6)` recebem metal oxidado, e `(6)` deixa de
exigir chave para permitir acesso ao boss após a arena. O instalador preserva
todos os outros blocos da cena. O gerador nunca salva a cena principal.

## Validação

`BasementBuilder.VerifyAndRender` verifica materiais, malhas, passagem da arena,
encaixe da porta no prefab e na instância do mapa. Simula a ativação do encontro,
12 inimigos, trava com um sobrevivente, desbloqueio após a última morte,
exclusão do boss e levantamento do collider da porta.

A simulação roda em modo de edição. Movimentação, dificuldade e comportamento
da IA existente precisam de playtest no jogo, especialmente junto aos pilares
e equipamentos; o sistema atual de inimigos persegue o player diretamente.

## Correção das passagens para o player

O `CharacterController` do prefab atual do player mede **2,120974 m de altura** e
**0,473301 m de diâmetro** em coordenadas de mundo. A primeira versão tinha apenas
2,15 m de altura na porta de combate, cerca de 2 m no corredor e descontinuidades
no piso da curva. A escala não uniforme da instância na cena também foi considerada.

As aberturas agora têm pelo menos 3 m de altura e largura antes do acabamento.
O batente da porta de combate deixa cerca de 2,76 m de largura útil. O corredor
em L tem **3 m de largura livre e 3 m de altura nominal**, com luminárias discretamente
abaixo do teto, piso contínuo e ligação até a sala do boss. Uma soleira no nível
da arena dá apoio ao player na entrada. O tamanho do player não foi alterado.

Os fragmentos antigos `Hallway*` ficam desativados para referência. A estrutura
nova está em `BossRoom/Player sized corridor`, e a soleira em
`PreBossArea/Player entrance threshold`. A cena restaura o cabeçalho da saída da
arena para a porta de combate se ajustar corretamente; duas paredes antigas
adicionadas à instância ficam desativadas. As demais salas e a posição do porão
no mapa são preservadas.

As folhas de `DoorPrefab (4)` e `(6)` também são centralizadas e redimensionadas
para cobrir as novas aberturas, com 3 m de altura e pequena folga lateral. Isso
mantém a porta de entrada vedada quando fechada, mesmo com a abertura mais larga.
Os controles de chave e de abertura dessas portas continuam nos scripts existentes.

`BasementPassageBuilder.cs` contém a correção e o teste com o `CharacterController`
real. Na cópia isolada, copiar também esse arquivo para `Assets/Editor` e executar:

```
-batchmode -quit -executeMethod BasementPassageBuilder.Build
```

O instalador `install_passages.py` usa os snapshots
`.utmp/Basement-passage-before.prefab` e `.utmp/Basement-passage-before.unity`.
Sem argumentos, instala os blocos preservados na cópia isolada; `--install`
transfere a correção validada para o projeto principal. Recusa alterações
concorrentes no prefab e na cena.

`BasementPassageBuilder.Verify` testa ida e volta entre entrada, arena, porta,
curva e sala do boss com `CharacterController.Move`, além do bloqueio com a porta
de combate fechada. As portas de chave são simuladas abertas nesse teste.
Também atualiza a cena de inspeção e as prévias, e repete a validação do combate.
O teste roda no modo de edição; balanceamento e IA continuam sujeitos a playtest.
