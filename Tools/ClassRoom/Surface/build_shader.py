"""Derive the surface shader from the installed URP Lit passes, without editing URP."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
package = next((ROOT / 'Library/PackageCache').glob('com.unity.render-pipelines.universal@*/Shaders/Lit.shader'))
target = ROOT / 'Assets/TotalChaos/Shaders'
target.mkdir(parents=True, exist_ok=True)
source = package.read_text(encoding='utf-8')
source = source.replace('Shader "Universal Render Pipeline/Lit"', 'Shader "UniDoom/TotalChaosInspiredSurface"')
source = source.replace('_WorkflowMode("WorkflowMode", Float)', '[HideInInspector] _WorkflowMode("WorkflowMode", Float)')
source = source.replace('_SpecColor("Specular", Color) = (0.2, 0.2, 0.2)', '[HideInInspector] _SpecColor("Surface controls", Vector) = (0, 0, 1, 1)\n        _DirtMap("Cavity Dirt Mask (white = clean)", 2D) = "white" {}')
source = source.replace('#pragma shader_feature_local_fragment _SPECULAR_SETUP', '// Metallic workflow only; _SpecColor stores surface controls.')
source = source.replace('"Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"', '"TotalChaosSurfaceInput.hlsl"')
source = source.replace('CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.LitShader"', 'CustomEditor "TotalChaosMaterialGUI"')
(target / 'TotalChaosInspiredSurface.shader').write_text('// Derived from URP 17.5 Lit.shader; Unity Companion License. See LICENSE-Unity.md.\n' + source)
(target / 'LICENSE-Unity.md').write_bytes(package.parents[1].joinpath('LICENSE.md').read_bytes())
print('Generated native URP pass integration:', target / 'TotalChaosInspiredSurface.shader')
