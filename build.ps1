$csc  = (Get-ItemProperty 'HKLM:\SoftWare\Microsoft\NET Framework Setup\NDP\v3.5').InstallPath+"csc.exe"
$cm3d2= (Get-ItemProperty 'HKCU:\Software\KISS\�J�X�^�����C�h3D2').InstallPath
$reip = Read-Host "ReiPatcher�̈ʒu�����/��s�ŃX�L�b�v"
if($reip){
  iex("{0} /t:library /lib:{1} /r:ReiPatcher.exe /r:mono.cecil.dll /r:mono.cecil.rocks.dll CM3D2.ArchiveReplacer.Patcher.cs" -f $csc , $reip)
}else{
  echo "Patcher�̃r���h�����܂���"
}
iex("{0} /t:library /lib:{1} /r:CM3D2x64_Data\Managed\Assembly-CSharp-firstpass.dll CM3D2.ArchiveReplacer.Hook.cs" -f $csc , $cm3d2)