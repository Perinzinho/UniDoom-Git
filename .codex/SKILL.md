---
description: Define realistic, oppressive survival-horror environments
  inspired by the classic Total Chaos Doom II mod on ModDB. Use when
  designing Unity scenes, materials, textures, lighting, props,
  environmental storytelling, or visual-quality reviews.
name: total-chaos-visual-direction
---

# Skill: Direção visual de survival horror inspirada em Total Chaos (ModDB)

## Objetivo

Orientar a criação de um jogo de terror em primeira pessoa com cenários
e materiais mais realistas, sujos, decadentes e fisicamente
convincentes, usando **Total Chaos --- a versão clássica do mod de Doom
II publicada no ModDB --- como referência visual**, sem copiar seus
assets, mapas ou identidade visual literalmente.

A meta não é reproduzir gráficos AAA modernos nem fazer um jogo low-poly
estilizado. A meta é um **realismo áspero e artesanal**: arquitetura
legível, superfícies com história, geometria funcional, iluminação
motivada por fontes plausíveis, paleta contida e detalhes perturbadores
usados com intenção.

## Referência e interpretação

A página do ModDB descreve Total Chaos como um survival horror situado
em Fort Oasis, uma ilha isolada ligada a uma comunidade de mineradores
desaparecida. O jogo percorre apartamentos, túneis de serviço, centros
comerciais, áreas urbanas e minas. O mod combina recursos do GZDoom com
modelos 3D e texturas de alta resolução. A galeria do projeto e imagens
específicas, como "Hotel Lobby" e "Shader implementation", são
referências úteis para estudar a direção visual.

Links de referência: - Página principal:
https://www.moddb.com/mods/total-chaos - Galeria:
https://www.moddb.com/mods/total-chaos/images - Hotel Lobby:
https://www.moddb.com/mods/total-chaos/images/hotel-lobby - Shader
implementation:
https://www.moddb.com/mods/total-chaos/images/shader-implementation -
Downloads / Director's Cut e Retro Edition:
https://www.moddb.com/mods/total-chaos/downloads

**Importante:** a descrição abaixo é uma interpretação de direção
artística para um projeto novo. Não presuma que cada técnica recomendada
aqui seja uma implementação interna exata do mod. Observe capturas de
tela reais da versão clássica antes de tomar decisões finais.

## Princípios inegociáveis

1.  **Realismo por material e composição, não por excesso de
    polígonos.** A forma precisa ser correta à distância de jogo; a
    textura, a rugosidade, as bordas gastas, a sujeira e a luz fornecem
    a leitura próxima.
2.  **Todo cenário já teve uma função.** Um corredor, sala de aula,
    apartamento, túnel ou escritório deve parecer construído e usado por
    pessoas antes do abandono.
3.  **A decadência deve ter causa e direção.** Água escorre para baixo;
    ferrugem se concentra perto de umidade e junções; tinta descasca em
    bordas e zonas de atrito; poeira se acumula em superfícies
    horizontais protegidas.
4.  **O terror nasce do que se vê parcialmente.** Preserve áreas de
    sombra, enquadramentos obstruídos, profundidade e pontos de
    interesse pouco visíveis, sem tornar o jogo ilegível.
5.  **Cada cenário tem identidade visual própria.** Não aplique a mesma
    textura, névoa, cor e intensidade de luz a todos os ambientes.
6.  **Detalhes têm hierarquia.** Silhueta e composição primeiro;
    materiais e iluminação em seguida; pequenos objetos e microdetalhes
    por último.
7.  **Consistência física.** Escala, espessura, encaixes, contato com o
    chão, direção da luz e comportamento dos materiais devem fazer
    sentido.

## Padrão de qualidade dos cenários

### Geometria e escala

-   Use escala métrica consistente na Unity; portas, corrimãos, mesas,
    interruptores e equipamentos devem corresponder a dimensões
    plausíveis.
-   Evite paredes perfeitamente planas e arestas matematicamente
    perfeitas em locais antigos. Use chanfros discretos nas bordas que
    captam luz e pequenas irregularidades onde forem plausíveis.
-   Dê espessura real a portas, batentes, chapas metálicas, bancadas,
    grades e caixilhos. Evite superfícies de papel quando a câmera pode
    se aproximar.
-   Construa a arquitetura com módulos reutilizáveis, mas quebre a
    repetição com variação de objetos, sujeira, danos, remendos e
    pequenas diferenças de posicionamento.
-   Crie uma leitura clara de primeiro plano, plano médio e fundo. Use
    portas, pilares, móveis, cabos, grades e vãos para criar
    profundidade, sem bloquear injustamente a navegação.
-   A geometria danificada deve corresponder à causa: concreto lascado
    em quinas, gesso quebrado ao redor de infiltrações, metal empenado
    por impacto, madeira inchada pela água.

### Texturas e materiais

Use materiais PBR sempre que a pipeline permitir: Base Color/Albedo,
Normal, Roughness (ou Smoothness invertida conforme a pipeline),
Metallic e mapas adicionais apenas quando necessários.

**Resolução sugerida, não obrigatória:** - Elementos grandes vistos de
perto --- paredes principais, piso de corredor, portas de destaque: 2K
como ponto de partida; 4K somente quando o enquadramento e a plataforma
justificarem. - Objetos médios --- armários, mesas, máquinas,
mobiliário: 1K--2K. - Props pequenos e repetidos --- papéis,
ferramentas, interruptores: 512--1K. - Atlas e trim sheets são
preferíveis para objetos repetidos e arquitetura modular, desde que
mantenham escala texel coerente.

Regras de textura: - Evite materiais uniformes e limpos em grandes
superfícies. Combine cor-base sutilmente variada, rugosidade, manchas e
desgaste localizado. - Concreto: poros, pequenas cavidades, reparos,
escurecimento por umidade e bordas quebradas; rugosidade alta, reflexo
difuso e quase nenhum brilho metálico. - Reboco e tinta: camadas
antigas, descascamento nas bordas e ao redor de janelas, marcas de fita,
diferenças de cor entre áreas expostas e protegidas. - Metal pintado:
pintura lascada nas quinas e pontos de contato; oxidação onde a água se
acumula; brilho somente em áreas polidas pelo uso. - Aço enferrujado:
variação de tons terrosos e escuros, rugosidade irregular; não use
laranja saturado como cobertura universal. - Madeira: veios na escala
correta, inchaço, rachaduras, manchas e acabamento desgastado em bordas;
madeira molhada pode escurecer e refletir um pouco mais. - Azulejo:
rejunte escurecido, peças rachadas ou ausentes, depósitos minerais e
variação controlada de brilho. - Vidro: sujeira, riscos, manchas e
reflexos discretos; use transparência onde a leitura através dele for
importante. - Tecido: fibras sugeridas pela textura, dobras, manchas,
desbotamento e bordas desfiadas; evite aparência plástica. - Borracha e
plástico envelhecido: brilho baixo a moderado, riscos, poeira em
cavidades e descoloração irregular. - Sangue, lodo e matéria orgânica
devem ser usados como acentos narrativos, não como substitutos para a
construção de materiais e atmosfera.

### Desgaste com lógica

Use três escalas de detalhe: - **Macro:** áreas de infiltração,
desabamento, corrosão extensa, placas faltando, entulho. - **Médio:**
descascados, rachaduras, manchas de mãos, trilhas de sujeira, riscos e
reparos. - **Micro:** poeira, pequenos arranhões, grãos, marcas de uso e
variação de rugosidade.

Não distribua dano uniformemente. Concentre-o em lugares coerentes: em
torno de vazamentos, perto do chão, nas quinas, em pontos de contato,
atrás de móveis, perto de ventilação e em áreas expostas ao clima. Use
decals com parcimônia e evite repetir o mesmo decal de forma
reconhecível.

## Iluminação: regras práticas

### Filosofia

A iluminação deve parecer produzida por fontes reais e, ao mesmo tempo,
controlar a tensão. Use contraste e áreas de sombra com intenção; não
deixe toda a cena igualmente clara nem enterre tudo em preto. O jogador
deve conseguir ler o espaço, antecipar obstáculos próximos e identificar
pistas importantes.

### Fontes de luz

-   Toda luz relevante deve ter uma explicação no cenário: luminária
    fluorescente, lâmpada incandescente, lanterna, luz do exterior,
    emergência, fogo ou equipamento.
-   Em locais abandonados, varie estados: luzes mortas, uma luminária
    que ainda funciona, fluorescente instável, lâmpada fraca, luz
    vazando por uma porta.
-   Evite colocar luzes invisíveis apenas para iluminar tudo. Se
    precisar de luz de preenchimento artística, mantenha-a discreta e
    preserve a lógica visual.
-   Faça a temperatura de cor reforçar a narrativa: luz fria e
    esverdeada em áreas institucionais ou industriais; âmbar sujo em
    lâmpadas antigas; luz natural fria e desaturada em exteriores
    nublados. Não transforme toda a cena em uma única cor.
-   Luzes piscantes devem ser raras e legíveis, com frequência e duração
    controladas. Não faça o cenário inteiro piscar constantemente.

### Exposição, sombras e contraste

-   Ajuste a exposição para preservar detalhe em sombras e realces.
    Evite janelas completamente brancas e cantos sem qualquer informação
    visual.
-   Sombras de contato são essenciais sob móveis, nas junções de parede
    e chão, atrás de objetos e em áreas onde itens se encostam.
-   Use iluminação indireta e ambient occlusion com moderação: AO deve
    reforçar contato, não criar halos escuros ao redor de todo objeto.
-   Sombras suaves podem funcionar em luzes amplas ou difusas; luzes
    pequenas e próximas geram sombras mais marcadas. Mantenha a direção
    coerente entre objetos.
-   A luz da lanterna deve revelar relevo e textura, criar queda de
    intensidade natural e não iluminar a sala inteira como um holofote
    omnidirecional. Evite um círculo branco chapado sem detalhe.
-   Use volumetria/neblina somente quando houver motivo visual, como
    poeira suspensa, vapor, fumaça ou profundidade atmosférica. Ela deve
    responder à luz e não funcionar como uma camada cinza universal.
-   Reflexos são seletivos. Piso molhado pode refletir fontes de luz;
    concreto seco e tecido não devem parecer espelhados.
-   Não confie apenas em pós-processamento para criar o visual. A
    composição, os materiais e as fontes de luz devem funcionar antes de
    aplicar color grading.

### Perfis por tipo de cenário

**Interior abandonado, corredor ou sala** - Base de luz baixa, com uma
ou duas fontes dominantes. - Pequenas áreas de luz quente ou fria
contrastando com sombras profundas. - Vazamento de luz sob portas e
através de janelas; iluminação indireta discreta. - Paredes e chão
precisam continuar distinguíveis nas sombras.

**Área institucional, como universidade ou hospital** - Fluorescentes
frias, algumas apagadas, tonalidade levemente esverdeada apenas quando
fizer sentido. - Reflexos controlados em azulejo, linóleo e metal; luz
irregular em salas com manutenção negligenciada. - Sinalização, placas e
equipamentos devem ser legíveis sem parecer recém-instalados.

**Túnel, porão ou instalação industrial** - Fontes espaçadas, pools de
luz separados e trechos de escuridão entre eles. - Tubulações, grades e
cabos podem projetar sombras interessantes. - Umidade concentrada em
zonas baixas ou perto de vazamentos; reflexos localizados, não piso
inteiro brilhante. - Névoa e vapor em áreas específicas, com silhuetas
visíveis à distância.

**Exterior nublado ou zona urbana abandonada** - Luz natural difusa,
paleta desaturada e sombras menos duras. - Contraste entre céu
relativamente claro e interiores escuros. - Vegetação, concreto e metal
com respostas materiais distintas. - Evite cobrir tudo com neblina
opaca: mantenha pontos de referência e camadas de profundidade.

**Mina, caverna ou espaço subterrâneo** - A escuridão deve reforçar a
escala e a incerteza, mas a navegação deve ter pistas visuais. - Fontes
locais pequenas, luzes de trabalho, lanternas e reflexos minerais
discretos. - Silhuetas e contornos devem aparecer antes de o jogador ver
todos os detalhes. - Evite paredes negras uniformes; use variação sutil
de material e luz rasante para comunicar forma.

## Direção por cenário: o que construir

### Apartamentos e áreas residenciais

-   Papel de parede desbotado, tinta descascada, portas deformadas,
    móveis deslocados, colchões, armários e objetos pessoais.
-   Indícios de ocupação anterior: marcas mais limpas onde havia
    quadros, fotografias caídas, copos, recibos, roupas e sinais de
    saída apressada.
-   Não encha cada sala de entulho. Reserve áreas relativamente vazias
    para tensão.

### Corredores, escadarias e edifícios institucionais

-   Repetição arquitetônica com variação de lâmpadas, portas, placas,
    carrinhos, extintores, bebedouros e avisos antigos.
-   Rodapés gastos, quinas lascadas, sujeira nos cantos e marcas de
    arrasto.
-   Perspectivas longas, portas entreabertas e luz no fim do corredor
    para compor enquadramentos.

### Túneis e áreas de manutenção

-   Conduítes, canos, válvulas, suportes, cabos, grelhas, caixas
    elétricas e sinalização industrial.
-   Depósitos minerais, ferrugem e água seguindo a gravidade.
-   Variação de altura, diâmetro e material das tubulações; não use o
    mesmo cano repetido sem lógica.

### Centro comercial ou espaços públicos

-   Vitrines sujas, letreiros parcialmente apagados, grades metálicas,
    lojas fechadas, quiosques, placas e mobiliário público.
-   Áreas abertas devem contrastar com passagens estreitas e espaços de
    serviço.
-   Distribua o desgaste segundo a exposição, o tráfego e a manutenção
    passada.

### Minas e espaços de extração

-   Madeira de escoramento, trilhos, carrinhos, equipamentos de
    mineração, cabos, rocha em camadas e estruturas de suporte.
-   Poeira seca em zonas protegidas e lama/umidade perto de
    infiltrações.
-   A arquitetura deve sugerir peso, instabilidade e profundidade, não
    apenas paredes rochosas genéricas.

## Modelos 3D e props

-   Crie silhuetas reconhecíveis em primeira pessoa e à distância;
    detalhe extra só quando a câmera se aproxima.
-   Use bevels discretos em arestas que recebem highlights. Bordas
    infinitamente afiadas deixam objetos artificiais.
-   Separe materiais por peça quando necessário: metal, borracha, vidro,
    tecido, madeira e sujeira não devem compartilhar uma resposta de luz
    idêntica.
-   Garanta que os objetos encostem no chão e em superfícies; corrija
    flutuação, clipping, pivôs e colisões.
-   Varie versões de props comuns: cadeira inteira, cadeira caída,
    cadeira quebrada; armário fechado e aberto; luminária funcionando e
    danificada.
-   Prefira objetos que contam história e parecem utilizáveis a
    decoração aleatória.
-   Inimigos e elementos orgânicos podem ser grotescos, mas devem
    compartilhar a mesma iluminação e escala do cenário. Evite aparência
    de asset limpo inserido no ambiente.

## Composição e narrativa ambiental

-   Planeje cada sala com um foco primário, um foco secundário e zonas
    de repouso visual.
-   Use silhuetas, portas, corredores, janelas e luzes para guiar o
    olhar sem setas artificiais.
-   Conte o passado do lugar com objetos e alterações físicas: cadeira
    bloqueando uma porta, ferramentas abandonadas junto a um reparo
    incompleto, marcas de arrasto, documentos molhados.
-   Aplique sinais de horror de forma seletiva. Um único detalhe
    perturbador bem enquadrado costuma ser mais eficaz que dezenas de
    manchas explícitas.
-   Use repetição com variação para criar desconforto: portas
    semelhantes, lâmpadas falhando em sequência, objetos familiares
    deslocados.
-   Preserve rotas de navegação e leitura de gameplay. Não esconda itens
    essenciais em sombras que parecem pretas em monitores comuns.

## Implementação na Unity

-   Primeiro confirme a pipeline do projeto: URP, HDRP ou Built-in. Não
    presuma que recursos e shaders de uma pipeline funcionam
    automaticamente em outra.
-   Prefira materiais PBR e iluminação coerente com a pipeline
    selecionada.
-   Use lightmaps/baked lighting para geometria estática quando fizer
    sentido; use luzes realtime para lanterna, luzes móveis e elementos
    dinâmicos. Uma solução mista costuma equilibrar custo e atmosfera.
-   Configure light probes para objetos dinâmicos em ambientes baked e
    reflection probes somente onde reflexos realmente acrescentam
    leitura.
-   Use volumes de pós-processamento com moderação: color grading,
    vinheta discreta, film grain muito sutil e bloom controlado. O bloom
    não deve apagar lâmpadas nem esconder textura.
-   Teste a cena com a exposição e brilho reais esperados para o
    jogador. Verifique em janela, tela cheia e diferentes condições de
    iluminação do monitor.
-   Use LODs, occlusion culling, instancing e colisores simplificados
    quando necessários; não sacrifique a composição para perseguir
    detalhe invisível.
-   Não aplique uma única textura de alta resolução a tudo. Mantenha
    densidade de texel consistente e priorize as superfícies mais
    próximas da câmera.
-   Antes de adicionar efeitos caros, garanta silhueta, materiais,
    escala, composição e iluminação básica.

## Processo de produção recomendado

1.  **Bloqueio:** construa o layout com formas simples, escala real e
    caminhos legíveis.
2.  **Composição:** defina vistas, pontos de interesse, silhuetas e
    áreas de tensão.
3.  **Materiais-base:** escolha uma paleta curta e materiais principais
    coerentes.
4.  **Iluminação-base:** trabalhe com as fontes reais da cena antes de
    adicionar sujeira e efeitos.
5.  **Assets hero:** modele e texturize objetos que o jogador verá de
    perto.
6.  **Decadência:** aplique dano macro, médio e micro com lógica física.
7.  **Narrativa ambiental:** adicione objetos e pistas que expliquem o
    passado do local.
8.  **Polimento:** ajuste sombras, reflexos, névoa, pós-processamento e
    áudio visualmente sincronizado.
9.  **Otimização:** revise draw calls, luzes, sombras, overdraw,
    texturas e LODs.
10. **Revisão de horror:** teste a cena jogando, não apenas em
    screenshots; avalie orientação, ritmo, visibilidade e surpresa.

## Checklist de avaliação por cenário

Dê uma nota de 0 a 2 para cada item: 0 = ausente/ruim, 1 = parcial, 2 =
convincente.

-   [ ] A escala e a arquitetura parecem plausíveis.
-   [ ] Os materiais são distinguíveis sem depender apenas da cor.
-   [ ] O desgaste tem causas físicas e não está distribuído
    aleatoriamente.
-   [ ] A iluminação tem fontes reconhecíveis e direção coerente.
-   [ ] As sombras preservam atmosfera sem esconder a navegação.
-   [ ] A cena tem profundidade, silhuetas e enquadramentos
    interessantes.
-   [ ] Há sinais claros de uso anterior e abandono.
-   [ ] Os props têm escala, contato, pivôs e colisões corretos.
-   [ ] Os detalhes importantes são legíveis na resolução e distância de
    jogo.
-   [ ] O cenário possui identidade própria e não parece uma cópia de
    outro.
-   [ ] Os efeitos de pós-processamento não encobrem defeitos.
-   [ ] O desempenho é aceitável na plataforma-alvo.

**Critério de aprovação:** 18/24 ou mais, sem nota zero em escala,
materiais, iluminação, legibilidade ou desempenho. Um cenário que falha
nesses pontos deve ser corrigido antes de receber mais detalhes.

## Como a IA deve responder e trabalhar

Ao receber a tarefa de criar ou melhorar um cenário, a IA deve:

1.  Identificar o tipo de ambiente, a função original, o estado de
    abandono e o sentimento pretendido.
2.  Descrever a composição, os materiais principais, as fontes de luz e
    a lógica do desgaste antes de produzir assets.
3.  Separar requisitos essenciais de detalhes opcionais e considerar o
    orçamento de desempenho.
4.  Se gerar código para Unity, fornecer configurações aplicáveis à
    pipeline indicada e declarar dependências; não inventar APIs ou
    afirmar que testou algo que não executou.
5.  Se não souber a pipeline, perguntar uma única coisa essencial ou
    apresentar uma solução neutra e indicar o que precisa ser adaptado.
6.  Avaliar o resultado com o checklist e apontar correções concretas,
    não comentários vagos como "deixe mais realista".
7.  Usar as referências do ModDB para estudar atmosfera e tratamento
    visual, sem copiar diretamente mapas, texturas, modelos, monstros ou
    outros assets protegidos.

## Prompt operacional curto

"Projete este cenário como um ambiente de survival horror realista e
decadente, inspirado na direção visual da versão clássica de Total Chaos
do ModDB. Use arquitetura funcional, escala plausível, materiais PBR com
desgaste localizado e causal, objetos que contam a história do lugar,
iluminação motivada por fontes físicas e sombras profundas mas legíveis.
Diferencie os materiais por rugosidade, relevo e resposta à luz. Evite
low-poly estilizado, superfícies limpas demais, sujeira aleatória,
neblina universal, filtros excessivos e escuridão que prejudique a
navegação. Antes de finalizar, avalie a composição, os materiais, a
iluminação, a narrativa ambiental, a legibilidade e o desempenho."

## Fontes

-   ModDB --- página oficial do mod Total Chaos:
    https://www.moddb.com/mods/total-chaos
-   ModDB --- galeria de imagens:
    https://www.moddb.com/mods/total-chaos/images
-   ModDB --- Hotel Lobby:
    https://www.moddb.com/mods/total-chaos/images/hotel-lobby
-   ModDB --- Shader implementation:
    https://www.moddb.com/mods/total-chaos/images/shader-implementation
-   ModDB --- downloads, incluindo Director's Cut e Retro Edition:
    https://www.moddb.com/mods/total-chaos/downloads
