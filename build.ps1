sal csc ((Get-ItemProperty 'HKLM:\SoftWare\Microsoft\NET Framework Setup\NDP\v3.5').InstallPath+"csc.exe")
$cm3d2= (Get-ItemProperty 'HKCU:\Software\KISS\�J�X�^�����C�h3D2').InstallPath
if(!$reip){
  $reip = Read-Host "ReiPatcher�̈ʒu�����/��s�ŃX�L�b�v"
}
if($reip){
  csc /t:library /lib:$reip /r:ReiPatcher.exe /r:mono.cecil.dll /r:mono.cecil.rocks.dll CM3D2.ArchiveReplacer.Patcher.cs
}else{
  echo "Patcher�̃r���h�����܂���"
}
csc /t:library /lib:$cm3d2 /r:CM3D2x64_Data\Managed\Assembly-CSharp-firstpass.dll CM3D2.ArchiveReplacer.Hook.cs