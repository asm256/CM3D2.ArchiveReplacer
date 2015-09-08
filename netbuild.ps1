$web = new-object net.webclient
$gist = 'https://gist.githubusercontent.com/asm256/8f5472657c1675bdc77a/raw/'
$web.DownloadFile($gist+'CM3D2.ArchiveReplacer.Hook.cs' , 'CM3D2.ArchiveReplacer.Hook.cs')
$web.DownloadFile($gist+'CM3D2.ArchiveReplacer.Patcher.cs' , 'CM3D2.ArchiveReplacer.Patcher.cs')
iex($web.DownloadString($gist+'build.ps1'))