# Library — passagens estreitas e apagão da Key02

Revisão de dificuldade aplicada ao `Assets/_Project/Scenes/Library/Prefabs/Library.prefab`, sobre a [composição anterior](../EncounterRevision/README.md).

## Obstáculos e inimigos

Cada passagem das três barreiras principais recebeu dois arquivos e uma segunda barreira de duas estantes. Os novos móveis formam curvas em sequência e escondem o corredor seguinte. São seis grupos físicos, com 12 móveis adicionais, reaproveitando malhas e materiais existentes. O trajeto de aproximação da chave aumentou de 58,7 m para 71,7 m. A porta padrão do Cylinder, as mesas maiores, o espaço da sala e a posição da Key02 foram mantidos.

As três ondas passaram de 7 para 16 inimigos configurados: 4 no primeiro avanço, 5 na aproximação do arquivo e 7 após pegar a chave. O intervalo de spawn é 1,1 s. A proteção anterior continua: pontos a menos de 3 m do jogador aguardam até 5 s, sendo ignorados se continuarem ocupados. Assim, em uma partida o total efetivo pode ser menor que o configurado.

## Mecânica simples

Pegar a Key02 desliga três luminárias fluorescentes e a emissão de seus nove tubos por oito segundos. A energia retorna automaticamente, restaurando os estados anteriores. O efeito dispara uma vez, junto da onda de retorno. Luz das janelas, demais circuitos e a lâmpada da chave continuam ajudando a ler o espaço. Não exige novos comandos ou interface.

O comportamento está no `LibraryEncounterDirector` da própria Library, com luzes, tubos e duração editáveis no Inspector. Nenhum material compartilhado é modificado. Desativar o diretor durante o efeito também restaura o circuito.

A variante local de inimigo agora libera seus colisores após morrer, preservando o corpo visual. Isso evita que vários corpos tornem um corredor estreito permanentemente intransponível; os inimigos vivos continuam bloqueando a passagem. O Enemy global permanece igual.

## Validação

Unity 6000.5.7f1, em cópia isolada do projeto:

- Compilação, referências, scripts, materiais e malhas; carregamento do prefab na SampleScene real.
- NavMesh persistente rebakeada com os obstáculos; percurso completo de 71,7 m com folga de cápsula e 16 pontos de spawn com volumes livres e caminhos completos. [Relatório](validation.txt).
- Play Mode com CharacterController: entrada, ida até a Key02, coleta real com atualização do inventário/ícone e retorno pela porta. Ondas, perseguição, dano/morte e liberação da colisão dos corpos verificados. Apagão e restauração das luzes verificados. Os corpos permaneceram no ambiente durante o teste. [Relatório](play-validation.txt).
- Instalação protegida por hashes, preservando os fileIDs anteriores, todos os `.meta`, a estrutura original e os arquivos externos à revisão. [Auditoria](installed-validation.json).

Não foi feita uma partida completa para balanceamento de armas/munição, medição de FPS no hardware final ou build. A exceção preexistente de `UnityEditor.Search` ao entrar em Play Mode aparece no log; as verificações desta revisão passaram. Reflexos existentes foram mantidos, sem novo bake.

[Planta dos obstáculos e spawns](encounter-plan.svg) · [Captura no mapa](InMap-Obstacle.png) · [Aproximação da Key02](InMap-KeyApproach.png).

## Regeneração

As fontes de geração e testes ficam aqui, fora de Assets. `prepare.py` registra o estado inicial e sincroniza apenas os arquivos necessários com `.utmp/LibraryPreview`. `prepare_checks.py` adapta os testes da revisão anterior e copia as ferramentas para a área temporária desse projeto. O builder deve rodar somente nessa cópia isolada e depende de `LibraryBuilder` das ferramentas anteriores. O backup inicial `Library.prefab` permite recomeçar a geração; `LibraryPressureBuilder.Recheck` valida os resultados já gerados. `install.py` exige todas as etapas aprovadas e publica somente o prefab, os dois scripts runtime locais e a NavMesh, mantendo seus GUIDs.
