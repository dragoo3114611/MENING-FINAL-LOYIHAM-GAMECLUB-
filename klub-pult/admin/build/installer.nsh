; Oʻrnatishda Windows Firewall qoidasi: klient kompyuterlar serverga ulana olishi uchun
!macro customInstall
  nsExec::Exec 'netsh advfirewall firewall delete rule name="Klub Pult Server"'
  nsExec::Exec 'netsh advfirewall firewall add rule name="Klub Pult Server" dir=in action=allow program="$INSTDIR\Klub Pult.exe" enable=yes profile=any'
!macroend
!macro customUnInstall
  nsExec::Exec 'netsh advfirewall firewall delete rule name="Klub Pult Server"'
!macroend
