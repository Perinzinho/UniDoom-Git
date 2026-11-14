$path='Assets/_Project/Scenes/LawSchool/LawSchool.unity'
$s=[IO.File]::ReadAllText((Resolve-Path $path))
$docs=[regex]::Split($s,'(?=^--- !u!)',[Text.RegularExpressions.RegexOptions]::Multiline)
$names=@{}; $transforms=@{}
foreach($d in $docs){
  $h=[regex]::Match($d,'^--- !u!(\d+) &(\d+)'); if(!$h.Success){continue}
  $type=$h.Groups[1].Value; $id=$h.Groups[2].Value
  if($type -eq '1'){$m=[regex]::Match($d,'^  m_Name: (.+)$','Multiline');if($m.Success){$names[$id]=$m.Groups[1].Value}}
  if($type -in @('4','224')){
    $go=([regex]::Match($d,'^  m_GameObject: \{fileID: (\d+)\}','Multiline')).Groups[1].Value
    $father=([regex]::Match($d,'^  m_Father: \{fileID: (\d+)\}','Multiline')).Groups[1].Value
    $pos=([regex]::Match($d,'^  m_LocalPosition: (.+)$','Multiline')).Groups[1].Value
    $transforms[$id]=@{go=$go; father=$father; pos=$pos}
  }
}
$array=[regex]::Match($s,'(?s)^  keyLocations:\r?\n(.*?)^  excludedKeyAreas:','Multiline').Groups[1].Value
foreach($m in [regex]::Matches($array,'fileID: (\d+)')){
  $id=$m.Groups[1].Value; $t=$transforms[$id]; $chain=@(); $a=$id
  for($i=0;$i -lt 5 -and $a;$i++){ $v=$transforms[$a]; if(!$v){break}; $chain+= $names[$v.go]; $a=$v.father }
  Write-Output "$id $($t.pos) $($chain -join ' > ')"
}
