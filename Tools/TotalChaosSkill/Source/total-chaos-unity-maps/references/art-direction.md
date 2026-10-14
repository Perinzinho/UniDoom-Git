# Direção de arte — estilo "Total Chaos" (versão GZDoom / ModDB)

Leia este arquivo antes de decidir qualquer cor, luz ou textura. O objetivo é reproduzir a
**linguagem visual e o clima**, não os assets: nunca copie texturas, modelos, sons ou mapas do
mod original. Use texturas procedurais (o builder já gera), assets CC0 ou assets do próprio usuário.

## Índice
1. O que é a referência
2. Pilares visuais
3. Paleta por tema
4. Luz
5. Névoa
6. Texturas e materiais
7. Pós-processamento
8. Props e storytelling ambiental
9. Som e ambiência (para completar o clima)
10. Anti-padrões
11. Variante "Retro" (baixa resolução)

---

## 1. O que é a referência

Total Chaos é um total conversion de survival horror para Doom II, rodando no GZDoom, feito por
Sam Prebble (wadaholic): ideia de 2008, desenvolvimento a partir de 2012, versão completa em
outubro de 2018 (premiado com um Cacoward em 2018). Existe também uma edição "Retro" em resolução
menor e um Director's Cut. Em 2025 o projeto virou um jogo standalone separado (Trigger Happy);
**esta skill mira o visual da versão ModDB/GZDoom**, mais áspero e "feito de setores" do Doom.

Cenário: a ilha fictícia de Fort Oasis, antiga colônia de mineração de carvão, abandonada de
forma misteriosa, castigada pelo vento e pelo mar. O jogador atravessa apartamentos, túneis de
serviço, prédios e o centro da cidade; a natureza começa a retomar as áreas subterrâneas. A
história vem de cartas espalhadas e de uma voz no rádio. Munição é escassa de propósito.

Influências declaradas pelo autor: Silent Hill, Resident Evil, Amnesia e até Dark Souls. A
imprensa descreveu o visual como texturas imundas, paleta contida e atmosfera sutilmente
perturbadora, próxima dos primeiros Silent Hill.

Herança técnica do motor Doom que dá a "assinatura": arquitetura em blocos ortogonais com pisos
e tetos planos, portas que sobem, luzes dinâmicas, névoa, modelos 3D para criaturas/armas.

## 2. Pilares visuais

1. **Sujeira e decadência.** Tudo está encardido: concreto manchado, ferrugem, mofo, água
   escorrendo. Nada é limpo, nem as luzes.
2. **Paleta contida e dessaturada.** Cinza-esverdeado, bege sujo, marrom-ferrugem. Cor viva só
   como acento raro (lâmpada âmbar, sangue, painel de emergência).
3. **Névoa e escuridão.** Visibilidade curta, ambiente quase preto, o jogador vive em poças de
   luz. O medo vem do que a névoa esconde.
4. **Luz motivada e escassa.** Toda luz tem fonte visível (lâmpada, janela, lanterna, painel).
   Lâmpadas piscam ou morrem. Salas seguras são as mais iluminadas.
5. **Granulado analógico.** Grain, vinheta forte, leve aberração cromática; sensação de
   fita/filme antigo.
6. **Arquitetura opressiva.** Concreto, blocos de apartamentos, túneis, tubulações; tetos baixos
   (~3 m), corredores de 1 célula (2 m), salas que parecem pequenas demais.
7. **Narrativa ambiental.** Cartas, rastros de sangue, barricadas, móveis virados, objetos
   deixados às pressas. Ausência de pessoas é a presença mais forte.
8. **Escassez visível.** Poucos itens, bem escondidos, em lugares perigosos.

## 3. Paleta por tema (presets do `TCTheme.cs`)

| Tema | Parede | Piso | Teto | Luz | Fog | Densidade | Saturação |
|---|---|---|---|---|---|---|---|
| `apartments` | `#5c5a50` | `#3b3731` | `#4a473f` | `#e0b56a` âmbar sujo | `#171916` | 0.050 | -30 |
| `tunnels` | `#4b504e` | `#2d302f` | `#3a3e3c` | `#a8d0cf` fria, esparsa | `#0d1211` | 0.075 | -40 |
| `streets` | `#6a6d68` | `#3f413d` | `#555853` | `#d9c58f` poste | `#5d666b` névoa de mar | 0.030 | -35 |
| `facility` | `#55493d` | `#2c2824` | `#3a332c` | `#e8903f` laranja industrial | `#1d150f` | 0.060 | -25 |

Regra de ouro: **valor (claro/escuro) importa mais que matiz.** Se a captura em escala de cinza
não tiver poças de luz e áreas quase pretas, o mapa está claro demais.

Acentos permitidos (usar com parcimônia, ~2% da tela): vermelho-sangue `#5a1414`, verde tóxico
`#6f8f3a`, azul-cinza de emergência `#6b8fa3`, amarelo-aviso desbotado `#a38f2e`.

Para escolher o tema: apartamentos/interiores domésticos → `apartments`; subsolo, esgoto, dutos →
`tunnels`; ruas e praças ao ar livre (usar `_` no grid) → `streets`; minas, fábricas, usinas →
`facility`. Um mapa mistura tema por mapa, não por sala; se precisar de transição, crie dois
mapas/cenas ou ajuste a cor das luzes por região depois.

## 4. Luz

- Ambient quase preto (`#0f100e` a `#363d42` só em exteriores). **Nunca** deixe ambiente alto
  para "resolver" cena escura: use luz motivada.
- Point lights: intensidade ~2–2.6 (URP), alcance 8–10 m, **sem sombras** na maioria
  (custo). Ative sombras só em 2–4 luzes-chave por área (sala segura, boss, set piece).
- Espaçamento: ≈ 1 luz por 20–40 m² de área andável nos interiores; **deixe zonas sem luz**
  (a lanterna do jogador cobre) — corredores longos com 1 luz a cada 3–4 células e trechos
  mortos entre elas funcionam muito bem.
- 30–50% das luzes devem piscar (`TCFlickerLight`); em túneis, mais ainda.
- Contraste de cor: luz quente em salas "vivíveis", fria em áreas hostis/industriais.
- Exteriores: sol quase inexistente (céu encoberto), luz direcional fria e fraca (0.4–0.5) +
  postes âmbar. O mar/vento sugere céu pesado, não pôr do sol bonito.
- Lanterna: Spot Light no jogador (ângulo 55–65°, alcance 15–18 m, intensidade ~3–4, sombras
  Hard). É a luz mais importante do jogo; o mapa deve ser projetado para ela.

## 5. Névoa

- `FogMode.ExponentialSquared`, cor = cor de fundo da câmera (sem horizonte visível).
- Densidade ≈ 0.05 em interiores, 0.07+ em túneis, 0.03 em ruas. Distância em que a visibilidade
  cai a ~10%: ≈ 1.5 / densidade (0.05 → 30 m; 0.075 → 20 m).
- Para "neblina tóxica" localizada: partículas lentas (Particle System, cinza-esverdeado, alfa
  baixo) em salas específicas, não fog global mais denso.

## 6. Texturas e materiais

- Resolução baixa e **filtro Point**: 128 px por tile cobrindo ~3 m (≈ 43 px/m) dá o aspecto
  de textura de jogo antigo. Não use 2K "limpas".
- Camadas de sujeira: base de ruído médio + **manchas verticais** (umidade escorrendo de
  cima) + **manchas largas** nos cantos/baixo + granulado de pixel. O gerador procedural faz isso.
- Juntas de painel/azulejo a cada 1,5–3 m quebram a monotonia do concreto.
- Smoothness 0.05–0.15. Quase nada brilha, exceto poças d'água (smoothness 0.8) e metal molhado.
- Variação > repetição: use 2–3 variantes de parede por tema e alterne por sala. Decals de
  mancha, sangue, mofo, rachaduras, ferrugem por cima.
- Se o usuário tiver texturas próprias: arraste para o campo Base Map dos materiais gerados em
  `Assets/TotalChaosMaps/Generated/Materials` (o builder não sobrescreve materiais existentes) e
  mantenha Filter Mode = Point para o look retrô.

## 7. Pós-processamento (URP Volume global)

| Efeito | Valor |
|---|---|
| Vignette | intensidade 0.40–0.45, smoothness 0.6, preta |
| Film Grain | tipo Thin2, intensidade 0.5–0.6, response 0.8 |
| Color Adjustments | saturação por tema (-25 a -40), contraste +15–20, post-exposure -0.2 a -0.3 |
| Bloom | threshold 1.0, intensidade 0.2–0.3 (só as lâmpadas emissivas brilham) |
| Chromatic Aberration | 0.05–0.10 (sutil) |
| Opcional | Lift/Gamma/Gain levemente verde nas sombras; Motion Blur desligado; Depth of Field só em cutscenes |

Tudo isso é aplicado por `TCAtmosphere.Apply`. Para afinar, mexa nos valores do `TCTheme.cs` ou
direto no asset `TC_PostFX_<tema>` em `Generated/Profiles`.

## 8. Props e storytelling ambiental

Densidade de decoração por tipo de espaço (props por 100 m²):

| Espaço | Props | Exemplos |
|---|---|---|
| Apartamento | 8–14 | cama revirada, geladeira aberta, TV quebrada, cadeira tombada, mala, quadro torto, cartas na mesa |
| Corredor / escada | 3–6 | extintor caído, lixo, manchas de sangue arrastando, barricada parcial, caixa de correio |
| Túnel de serviço | 6–10 | tubulações, válvulas, caixas de fusíveis, poças, grades, fios soltos |
| Rua / praça | 4–8 | carro abandonado, poste caído, bancos, lixeiras, tapume, caixotes, folhas |
| Instalação/mina | 6–10 | vagonetes, caixotes, barris, guinchos, trilhos, ferramentas |

Natureza retomando: musgo e raízes nos tetos de subsolos, poças permanentes, mato nas rachaduras
do pavimento. Sinais de "alguém esteve aqui": porta barricada por dentro, rabisco na parede,
luz de lanterna esquecida (acende uma luz fria isolada num cômodo escuro).

Colete e posicione **pelo menos 1 carta/nota por região grande**; ela pode ser só um marcador
`I` com rótulo `note` — o texto fica a cargo do gameplay.

## 9. Som e ambiência (completa o clima mesmo sem pedido explícito)

Camada contínua baixa (vento no mar, ronco de ventilação), eventos esparsos (porta rangendo ao
longe, passos, rádio com chiado), reverb curto em salas pequenas e longo em túneis. Silêncio
total só antes de sustos. Sugira ao usuário um `AudioSource` ambiente por região; não gere áudio.

## 10. Anti-padrões (o que quebra o estilo)

- Saturação alta, cores "de jogo infantil", skybox azul bonito.
- Luz uniforme ou ambiente alto; tudo visível a 100 m.
- Salas grandes, vazias e simétricas; paredes planas sem sujeira nem desgaste.
- Props limpos e arrumados; ausência de sinais de passagem/abandono.
- Fog fraco que revela o limite do mapa; geometria limpa demais (sem pilares, vigas, tubos).
- Itens e inimigos espalhados uniformemente (sem ritmo; veja `level-design.md`).

## 11. Variante "Retro" (baixa resolução)

Para o visual ainda mais granulado da edição Retro: URP Asset → **Render Scale 0.5–0.6**,
Upscaling Filter = Point (ou renderizar para uma Render Texture 640×360 e exibir em tela
cheia com filtro Point), texturas 64 px, grain um pouco mais forte, sem MSAA. Mantenha a UI em
resolução nativa.
