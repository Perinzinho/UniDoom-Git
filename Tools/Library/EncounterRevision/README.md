# Library — obstáculos e busca da Key02

Alteração salva em `Assets/_Project/Scenes/Library/Prefabs/Library.prefab`. A entrada usa as mesmas malhas e materiais da porta da biblioteca em `Cylinder.prefab` (`Service bay 36`), mantendo abertura simples e destrancada.

## Percurso

Três barreiras atravessam o salão, alternando passagem pela direita, esquerda e direita. Reaproveitam 18 estantes, seis mesas tombadas e arquivos. Estantes altas interrompem a visão e impedem atalhos; peças baixas dão cobertura e permitem observar parte do próximo corredor. As onze mesas maiores foram acomodadas nos espaços entre barreiras. Colisores novos acompanham os volumes visíveis, com apoio no piso.

A Key02 fica sobre a mesa do fundo, destacada por uma pequena luz de leitura. Usa o prefab, inventário, interface e som de coleta existentes. A região de coleta permite alcançá-la pela borda da mesa. O trajeto calculado mede 58,7 m da entrada até a aproximação da chave; o teste em Play Mode percorreu ida, coleta e retorno pela porta.

Veja a [planta dos obstáculos e spawns](encounter-plan.svg), a [visão dos obstáculos no mapa](InMap-Obstacle.png), a [porta padronizada](InMap-Door.png) e a [aproximação da chave](Library-KeyApproach.png).

## Encontros

Sete marcadores, distribuídos em três ondas que disparam uma única vez por instância:

| Onda | Disparo | Pontos |
|---|---|---|
| Reference stacks ambush | Avançar além da primeira barreira | Advance A 1–2 |
| Archive approach | Avançar além da segunda barreira | Advance B 1–2 |
| Key02 return pressure | Coletar a Key02 desta instância | Return 1–3 |

O intervalo entre spawns é 1,4 s. Um ponto aguarda enquanto o jogador estiver a menos de 3 m; após 5 s ainda ocupado, é ignorado. Isso evita materializar inimigos sobre o jogador. O diretor e os marcadores ficam em `Abandoned Library Interior/Key retrieval encounter`; selecionar esse grupo exibe os volumes e pontos no Editor.

`LibraryZombie.prefab` é uma variante local do Enemy existente. Mantém vida, dano e morte, com um adaptador NavMesh para contornar os móveis. O ataque por distância é suspenso enquanto houver um obstáculo entre inimigo e jogador. O inimigo global não foi alterado. NavMesh persistente salva em `Navigation/EncounterRevision`, sem bake em runtime.

## Validação

- Unity 6000.5.7f1: compilação, materiais, malhas, scripts e referências serializadas passaram na cópia isolada do projeto; prefab também carregado na SampleScene real para revisão.
- Percurso completo com folga de cápsula; os sete pontos têm espaço físico e caminho completo até o objetivo. [Relatório](validation.txt) e [trajeto calculado](route.json).
- Play Mode real com CharacterController: entrada/saída pela porta, coleta da Key02 com atualização do inventário e ícone, três ondas e sete inimigos, perseguição por NavMesh e dano/morte existentes. Inimigos do teste removidos após verificar dano para não bloquear a avaliação do trajeto com corpos. [Relatório](play-validation.txt).
- Instalação por hash: 25.014 fileIDs existentes e os transforms da estrutura original preservados; nenhum GUID novo em conflito. SampleScene e todos os outros arquivos existentes ficaram intactos. [Auditoria](installed-validation.json).

Os testes não cobrem balanceamento de armas/munição, uma partida completa com combate, FPS no hardware final ou build. O Editor registrou a exceção preexistente `UnityEditor.Search.ArgumentOutOfRangeException` ao entrar em Play Mode; as verificações desta revisão passaram. Reflexos existentes foram mantidos, sem novo bake.

## Ferramentas

Os arquivos C# nesta pasta são fontes de geração/validação para o projeto isolado `.utmp/LibraryPreview`, onde se apoiam nas ferramentas anteriores de Library. Apenas `LibraryEncounterDirector.cs` e `LibraryZombieNavigator.cs` têm cópias runtime sob os assets de Library. Não executar o gerador no projeto principal. `install.py` protege o estado inicial por hash e publica somente os resultados de Library validados; para repetir uma geração, restaurar o backup `Library.prefab` nesta pasta na cópia isolada. Os logs separam a validação geométrica do teste final de coleta.
