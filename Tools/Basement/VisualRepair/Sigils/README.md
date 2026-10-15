# Pentagrama e um círculo de sigilos

Os selos no piso da pré-arena e da sala final voltaram ao pentagrama de cinco traços. Uma única faixa circular contém as três frases abaixo em sequência, com espaçamento maior entre as frases:

1. Abra sua percepção a uma infinidade superior
2. A sua mente torna-se invólucro inerte para poderes que te formam
3. Descubra e perceba o real propósito da sua própria entidade

Cada letra corresponde a um glifo do alfabeto fornecido pelo usuário, transcrito como traços vetoriais. Acentos e cedilha usam a letra-base; o hífen em “torna-se” usa o espaçamento de separação entre palavras. Aspas não são inscritas. A legenda vetorial de revisão está em [alphabet.svg](alphabet.svg); ela fica fora dos assets do jogo. [phrases.txt](phrases.txt) registra as frases e sua normalização.

As inscrições começam junto à marca inferior voltada à entrada. O topo dos glifos aponta para o centro; as palavras têm pequenos espaços. Dois contornos delimitam a única faixa de sigilos. Diâmetros externos preservados: 6,5 m na pré-arena e 14,3 m na sala final. A disposição mantém a mesma proporção nos dois tamanhos. Como as três frases ocupam a mesma linha, os glifos ficaram menores e devem ser examinados de perto.

## Implementação

`BasementSigilInscription.cs` gera meshes estáticos, combinados por material. Esta revisão altera somente o conteúdo das quatro malhas compartilhadas `Preparation_Contours`, `Preparation_Phrases`, `Sanctum_Contours` e `Sanctum_Phrases`. Os arquivos de prefab, a cena, GUIDs, materiais e componentes ficam idênticos ao snapshot anterior. Os dois renderers de cada selo usam os materiais URP Lit `RitualPigment` e `RitualChalk`, sem emissão. A sala final conserva a altura da pintura de 55 mm para respeitar o relevo das pedras; a pré-arena mantém 12 mm. O nome serializado legado do grupo final foi preservado para evitar mudanças na hierarquia.

Não há TextMesh, fonte, textura de lettering, script de runtime, collider ou luz adicional nas inscrições. Os letreiros removidos anteriormente continuam ausentes. Os assets novos ficam em `Assets/Art/Basement/SigilInscription`.

## Geração, validação e reversão

O projeto de trabalho isolado é `.utmp/BasementPreview`. O gerador exige esse ambiente. `single_circle.py prepare` salva o snapshot atual e sincroniza a cópia; a Unity executa `BasementSigilInscription.Build`; `single_circle.py stage` confere os IDs e restaura os bytes originais dos prefabs na cópia; uma sessão nova executa `BasementSigilInscription.Verify`; `single_circle.py install` verifica hashes e instala somente as quatro malhas alteradas. O snapshot existente nunca deve ser sobrescrito. `sigils.py` e `staging-report.json` registram a instalação inicial anterior.

A verificação inclui carregamento dos três prefabs, normais voltadas para cima, referências de mesh, ausência de textos convencionais, capturas dos dois selos, compilação dos shaders usados, travessia com CharacterController no mapa e no prefab e simulação do portão/encontro. Resultados atuais em [validation.txt](validation.txt), [verification.json](verification.json) e [single-circle-validation.json](single-circle-validation.json). Não houve medição de FPS nem novo Player build nesta alteração de arte.

O snapshot imediatamente anterior fica em `.utmp/BasementSingleSigilCircle/before`. Para reverter esta etapa, restaure somente as quatro malhas mencionadas desse snapshot, conferindo antes se houve alterações posteriores. A cena e os shaders não são modificados pelo instalador.
