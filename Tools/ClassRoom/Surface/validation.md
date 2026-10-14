# Validação da revisão PBR

Executada em 13/10/2026 na Unity 6000.5.7f1 / URP 17.5, em cópia isolada.

| Teste executado | Resultado |
|---|---|
| Compilação C# / shader e quatro capturas da sala | Passaram, sem erros de shader nos passes/variantes exercitados |
| SRP Batcher | Compatibilidade confirmada após renderizar o shader |
| Controles no render real | Dirt, wetness, saturação e contraste alteraram os pixels; detail/parallax opcionais renderizaram sem erro |
| Conversão / reversão | 12 materiais novos; 368 renderizadores revertidos aos originais; reflexão original restaurada |
| Play Mode | 120 frames em três posições; exposição fixa, drift de luz igual a zero |
| Transições e sombras | 82 amostras nos dois sentidos; zero reduções automáticas do atlas dentro da sala |
| Regiões | As seis regiões selecionaram seus parâmetros e restauraram ambiente, câmera, sol e tiers de sombra |
| CharacterController / porta | Rotas de 186 e 83 pontos em ida e volta; porta e herança na SampleScene passaram |

A preparação conserva os **2.986 IDs** do interior e os **61 IDs** do prefab
externo. Somente listas de materiais e a referência da reflexão são alteradas.
Geometria, colisores, iluminação, scripts, cena, player e materiais originais
permanecem preservados. Os PNGs baixados conservam os bytes verificados.

A nova captura do corredor tem luminância RGB mediana **0,0625**, frente a
**0,0619** antes desta revisão. A parcela abaixo de 0,02 aumenta de **3,66% para
12,46%**, refletindo a madeira fotografada mais escura e o contraste dos materiais;
piso e silhuetas permanecem visíveis nas capturas inspecionadas. O clipping das
quatro vistas finais é 0,00%. Esses números medem RGB de exibição, não iluminância.

Nenhuma luz ou geometria foi acrescentada ao mapa. As texturas aumentam de resolução,
com compressão, mipmaps e limite de 2K/1K. Parallax fica desativado nos materiais
aplicados. **FPS, tempos de GPU, combate e diferentes monitores não foram testados.**

Logs: `.utmp/ClassRoom-PBR-final.log` e `.utmp/ClassRoom-PBR-play.log`.
As consultas iniciais de SRP Batcher retornavam “Not initialized” antes do render;
o verificador foi corrigido para consultar após a inicialização. As primeiras
capturas também motivaram a calibração da madeira; não são o resultado final.
