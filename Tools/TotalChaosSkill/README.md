# Sala, porão e cilindro conforme a skill

Fonte aplicada: [total-chaos-unity-maps/SKILL.md](Source/total-chaos-unity-maps/SKILL.md),
fornecida pelo usuário em `total-chaos-unity-maps.skill`. Foram lidas as três
referências da skill antes da implementação. Os arquivos originais estão
preservados em `Source/`, incluindo os sete scripts Unity e as ferramentas Python.

O usuário confirmou **manter o traçado e ajustar a atmosfera**. Por isso esta
revisão aplica a direção de arte aos prefabs existentes, com a adaptação de
Volumes locais prevista em `references/unity-implementation.md §9`. Não utiliza
o gerador de grid para reconstruir a arquitetura e não troca `OpenDoorWithKey`
pelo exemplo `TCSimpleDoor` da skill.

## Atmosfera instalada

| Área | Tema | Névoa Exp² | Saturação | Exposição |
|---|---|---|---|---|
| Sala universitária | apartments | 0,050 | −30 | −0,2 EV |
| Cilindro coberto | apartments | 0,050 | −30 | −0,2 EV |
| Arena, corredor e chefão | facility | 0,060 | −25 | −0,2 EV |

O pós-processamento usa vinheta 0,42, Film Grain Thin2 0,55, contraste +18,
bloom 0,25 com threshold 1 e aberração cromática 0,08. A cor de fundo da câmera
acompanha a névoa. O ambiente fica quase preto e a luz direcional é desligada
enquanto a câmera está dentro dessas regiões.

As texturas autorais recebem **Point, limite de 128 px e anisotropia 1**. Os PNGs
originais de 512 px permanecem intactos; a mudança é de importação. As faces
com UV métrico usam aproximadamente um tile por 3 m. Materiais derivados
dessaturam concreto, ferrugem, madeira e tecido, limitam o brilho e deixam
vermelho como acento. O sangue usa `#5a1414`.

Preenchimentos sem fonte e reflexos de apoio foram apagados. As lâmpadas
artificiais sobreviventes têm alcance mais curto; cerca de 40% piscam ou apagam
brevemente com o `TCFlickerLight` original da skill. Sombras ficam restritas a
luzes-chave. Difusores visíveis marcam as fontes; a luminária do rádio tem
suporte e suspensão. Seu painel e texto foram trazidos 20 cm à frente da folha
decorativa para voltarem a ser legíveis, sem deslocar o colisor do móvel.

Uma **lanterna da câmera, alternada com F**, fica disponível dentro das regiões:
Spot Light de 60°, alcance 16 m, intensidade 3,5 e sombras Hard. Ela é criada
em runtime e não modifica o prefab do player. Ao sair das áreas, a lanterna
apaga e as configurações anteriores de névoa, ambiente, sol e câmera voltam.
O pós-processamento usa seis limites locais: sala, átrio oval, arena, duas
partes do corredor em L e sala do chefão.

Âncoras narrativas recebem `TCMapMarker` com rótulos `note_*`: última aula,
rádio, aviso das criaturas, evacuação fracassada e último aviso. São marcadores
de cenário; não acrescentam uma mecânica de leitura, chaves, loot ou inimigos.
O áudio existente é mantido. A recomendação da skill para uma camada ambiental
por região pode usar os assets sonoros do jogo numa revisão de áudio.

## Traçado aprovado e diagramas

- [SalaDeAula.json](Layouts/SalaDeAula.json) / [PNG](Layouts/SalaDeAula.png).
- [Cilindro.json](Layouts/Cilindro.json) / [PNG](Layouts/Cilindro.png).
- [Porao.json](Layouts/Porao.json) / [PNG](Layouts/Porao.png).

`prepare_existing_layouts.py` gera os esquemas e executa os dois scripts originais
de validação e preview da skill. Todos passaram sem erros bloqueantes. Eles
documentam conexões e pistas, com quantização de 2 m; os marcadores não instanciam
conteúdo novo no mapa real.

O validador avisa que cilindro e porão não têm ciclos entre regiões separadas por
portas. No cilindro ele não conta os dois circuitos contínuos do térreo, que são
percorridos pelo CharacterController real na validação Unity. O porão mantém a
sequência arena → comporta → corredor → chefão. Esses avisos são justificados
pela escolha explícita de preservar o traçado. As dimensões ampliadas e o teto
alto do chefão também seguem as preferências anteriores do usuário, em vez de
aplicar os tamanhos padrão para um mapa novo da skill.

## Validação e reprodução

Use Unity 6000.5.7f1 com URP, numa cópia isolada `.utmp/BasementPreview`.
O snapshot de referência fica em `.utmp/SkillAtmosphere-before`, com manifesto
SHA-256 dos assets atuais. Copie os scripts originais para
`Assets/TotalChaos/{Editor,Runtime}`, o adaptador Runtime desta pasta para
`Assets/TotalChaos/Runtime` e seus arquivos Editor para `Assets/Editor` da cópia.
Os geradores/verificadores anteriores das salas também precisam estar na cópia.

1. Execute `SkillAtmosphereBuilder.Build`, com log `Skill-atmosphere-build.log`.
2. Execute `python Tools/TotalChaosSkill/install_skill_atmosphere.py` para preparar
   os prefabs mantendo os blocos anteriores.
3. Execute `SkillAtmosphereVerifier.Verify`, `ClassRoomHorrorVerifier.VerifyAndRender`,
   `BasementFinalVerifier.Verify` e `CylinderDecayVerifier.Verify` em processos
   separados, com os logs `Skill-atmosphere-verify.log`, `Skill-classroom.log`,
   `Skill-basement.log` e `Skill-cylinder.log`.
4. Execute `SkillPlayModeVerifier.Run` sem `-quit`, com log `Skill-playmode.log`.
   O runner testa os efeitos em frames reais e encerra a Unity ao terminar.
5. Execute o instalador com `--install`. Ele recusa erros nos logs ou alterações
   concorrentes e instala os prefabs, os materiais, as configurações de importação
   e os oito scripts de suporte da skill/atmosfera.

O teste em Play Mode usa uma cena temporária baseada na SampleScene, com os
scripts de gameplay desabilitados para isolar a atmosfera. Essa cena de teste
não é instalada. A validação de circulação usa o CharacterController real e as
portas existentes. O comportamento de batalha continua sujeito a playtest.

A indexação de busca no startup foi desligada **somente na cópia temporária**,
para evitar uma exceção interna de `UnityEditor.Search` ao recarregar o domínio.
A preferência e seu papel são descritos na
[documentação oficial de Search](https://docs.unity.com/en-us/engine/6000.6/manual/unity-editor/editor-settings-reference/preferences/search).
As configurações de busca do projeto principal são preservadas.

As capturas `Skill-*.png` usam névoa, lanterna e pós-processamento reais. Os outros
cenários e os TextMesh antigos ficam ocultos apenas durante a captura focada;
as colisões da cena permanecem disponíveis. A cena principal e o player não
são salvos pelo gerador.
