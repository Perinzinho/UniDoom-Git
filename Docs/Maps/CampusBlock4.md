# Bloco 4 — Tecnologia da Informação

Atualizado: **2026-11-08**. Direção: `.codex/PROMPT_MESTRE.md` e `total-chaos-visual-direction` / `.codex/SKILL.md`. Esta ficha substitui as medidas e os testes da primeira revisão de 08/11. Histórico: [README_CAMPUS_EXTERIOR.md](../../README_CAMPUS_EXTERIOR.md), seção 12.

## Leitura rápida

- Cena `Assets/_Project/Scenes/CampusBlock4/CampusBlock4.unity`; prefab `Prefabs/CampusBlock4.prefab` na mesma pasta. Uma instância; sem cena/menu novos.
- **Hall de 15 m, quatro corredores de 8 m e 24 ambientes numerados 401–424.** Salas de **13,5 m** de largura; profundidade externa de **17 m**, interna de **11,75 m**, conforme os módulos medidos em LawSchool. Pé-direito aproximado de **4,06 m**. Player na escala original.
- Cruzamentos em `z = ±16,25`; salas entre `x = ±7,5` e `±48`; banheiros nas extremidades laterais. Largura externa próxima de **144 m**, frente em `z ≈ −48,3`, fundo em `z ≈ 37,55`. O total de salas é menor que o de Direito; a implantação não tem a mesma extensão de LawSchool.
- A espera/prolongamento do hall oposto à entrada foi removida; parede no limite do segundo cruzamento, `z = 20,25`. Salas externas ampliadas para 17 m; o espaço central posterior fica fora do interior acessível.
- **20 ambientes de ensino com computadores**, auditório 424, copa 401, coordenação 410 e depósito compacto 419. Secretaria e quatro banheiros adicionais. Nomes de matérias permanecem somente nos nomes internos dos objetos.
- Branco institucional, laranja à direita, portas/placas azuis, caixilhos escuros, piso quente, forro modular e fluorescentes preservam a identidade do Bloco 4. Materiais e fotografias originais inalterados.
- Mesas metálicas, cadeiras de escritório e notebooks já importados substituem mobiliário simplificado. Três colunas: 12 estações nas salas externas, nove nas internas, mais mesa docente. Quatro estações conservam os monitores especiais do enigma.
- Fontes e licenças: `Licenses/AssetSources/CampusBlock4/layout-revision.json`. Nenhum modelo externo novo baixado nesta revisão.

## Portas, catracas e textos

Catracas em `z = −42,15`, após a chegada. Quatro pedestais delimitam três folhas móveis, com passagem central mais larga. Vidros fixos e corrimãos completam a barreira até as paredes. O canto direito da entrada foi fechado para impedir desvio lateral.

As 24 portas numeradas usam vão de **1,75 × 3,075 m**, folha de **1,726 × 3,051 m** e folga nominal de **12 mm** por borda. Batentes, folhas e ferragens ajustados em conjunto; secretaria encaixada no vão próprio. Controladores originais e trava da 423 preservados. **28 controladores ativos**: 24 ambientes, secretaria e três passagens da entrada.

Retiradas as placas de “Bloco 4 / Ciências da Computação” e “catracas / passagem livre”. Frentes identificadas somente por **401, 402, …, 424**. Textos fixos rasterizados em `Textures/Printed campus lettering.png`, atlas RGBA persistente aplicado às superfícies por meshes com UVs; TMPs de origem desabilitados. O estado variável do teclado permanece dinâmico no LCD físico.

## Banheiros e apoio

Quatro conjuntos reproduzem geometria, materiais, pias, sanitários e disposição de cabines de **LawSchool**, sem modificar os assets de origem. Femininos à esquerda: **13,5 × 24,5 m**; masculinos à direita: **9,5 × 24,5 m**. Quatro cabines por conjunto, acessos pelos dois corredores. Folhas de cabine na posição estática aberta do modelo de Direito, sem controladores adicionais. Meshes exclusivos persistentes conservam UVs e materiais de origem.

| Ambiente | Localização | Uso |
|---|---|---|
| Secretaria | Esquerda da entrada | Atendimento e quinta pista com retrato original |
| Copa 401 | Primeiro corredor, esquerda | Pia, micro-ondas, bancada, mesas, cadeiras, armário e bebedouro |
| Coordenação 410 | Primeiro corredor, direita | Mesas com computadores, atendimento, arquivo e copiadora |
| Depósito 419 | Segundo corredor, direita | Pequeno espaço de cabos, periféricos e dispositivos, com estante e bancada |
| Sala 423 | Segundo corredor, direita | Porta do puzzle, chave e espingarda coletável |

O depósito permanece compacto dentro do módulo. Objetos substituídos ficam inativos para preservar fileIDs. Geometria remanescente das salas limpa de móveis e equipamentos sobrepostos.

## Puzzle e recompensas

Scripts e configuração originais **inalterados**. Pistas: 402 quadrado=1, 407 triângulo=2, 414 círculo=3, 420 losango=4 e secretaria cruz=5. Ordem junto ao quadro: triângulo, losango, quadrado, cruz, círculo; senha **24153**. A inscrição mostra somente os símbolos.

Quadro completo e quatro retratos conservam imagens, materiais e recortes UV originais. Geometria dos planos e alinhamento dos monitores corrigidos. Parede do quadro à direita do hall, próxima de `x = 5,85`, com face voltada ao hall.

Teclado junto à porta da 423: aproximadamente **(21,30; 1,50; 20,095)**. Chave perto de **(23,10; 1,27; 34,15)**; espingarda atrás, perto de **(23,10; 1,06; 35,65)**. Chave mantém sprite, billboard e inventário `block4.teacher-cell`; arma usa pickup global.

`Block4PuzzleProgress` preserva porta e chave durante a sessão. A chave coletada não reaparece. O pickup de espingarda é recriado ao recarregar a cena, conforme o sistema global, enquanto a arma carregada permanece no player persistente. Sem inventário paralelo ou gravação do puzzle em disco.

## Conexões

Chegada e `SpawnPoint`: **(0; 1,11; −44,55)**, ID `FromCampusExterior`. Saída: **(0; 1,4; −47,70)**, destino `CampusExterior / FromCampusBlock4`. Contratos do exterior preservados. Loading: `Resources/Textures/UI/Transitions/CampusBlock4.png` dentro da pasta da cena.

## Validação desta revisão

Unity **6000.5.7f1**, URP do projeto, Windows/D3D12, **AMD Radeon RX 6400**. Compilação, carregamento e salvamento via Unity API executados. Conferidos pisos dos 24 ambientes, folgas, numeração impressa, quatro banheiros, shaders, materiais, meshes, cinco pistas e referências das recompensas.

Comparação preservou os **13.041 registros/fileIDs anteriores do prefab**, os **21 da cena**, os `.meta` existentes, os materiais/fotografias/scripts/configuração protegidos do Bloco 4 e os assets de origem de LawSchool.

Estado final: **19.815 registros no prefab e 29 na cena**, 28 controladores de porta, **4.626 MeshRenderers ativos/habilitados** e **7.335.789 triângulos** somados em seus meshes antes do culling. Esses números incluem o mapa inteiro e não equivalem à geometria visível em um frame.

**Play Mode executado com o player real**, cápsula de aproximadamente **0,473 × 2,121 m**: nova partida pelo menu; tentativa de passagem com as catracas fechadas ao longo dos 15 m da entrada; abertura e travessia das três folhas; percurso do hall e dos quatro corredores; fechamento do acesso à antiga espera posterior. As **25 portas** dos ambientes e da secretaria foram abertas e atravessadas pelo `CharacterController`, assim como os **oito acessos** dos banheiros. Código errado conservou a trava; **24153** abriu a 423 e devolveu o controle. Chave e espingarda coletadas pelos triggers reais. Recarga da cena conservou porta, chave, espingarda equipada e um único jogador. Cabines foram conferidas visualmente na posição aberta do modelo de Direito; não houve travessia individual de cada cabine.

Capturas isoladas das outras cenas abertas, em **1600 × 900**, `output/Bloco4-Revisao-*.png`: entrada/catracas, hall, corredor, sala/porta 402, quadro, fim do hall, dois banheiros, recompensas 423, copa, coordenação e planta. Teto ocultado somente na captura da planta.

**Não executados:** build, recálculo de NavMesh/oclusão, benchmark CPU/GPU com Profiler e travessia real dos portais exterior/interior. Contagens não comprovam ganho de desempenho. Erros de ferramentas temporárias de outras cenas observados no Editor não foram atribuídos a esta revisão.

Revisão visual pela skill: escala, materiais, fontes de luz, sombras navegáveis, contato dos props, legibilidade, identidade e pós-processamento conferidos nas capturas. Desgaste causal, variedade de enquadramentos e sinais de abandono permanecem parciais na base atual; desempenho não recebeu nota confirmada sem medição dedicada. A última correção decorativa reconstituiu a malha plana compartilhada das fotografias e refez as capturas, sem mudar colisões, portas ou scripts após o Play Mode. Ferramentas pontuais retiradas após verificar resultados persistentes e ausência de dependências em seus GUIDs.
