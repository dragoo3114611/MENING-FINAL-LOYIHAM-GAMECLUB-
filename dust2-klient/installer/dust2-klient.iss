; DUST2 Klient agent — Inno Setup o'rnatish skripti
; Yig'ishdan oldin agent quyidagi papkalarga publish qilinadi:
;   agent\publish\service\Dust2Agent.Service.exe
;   agent\publish\shell\Dust2Agent.Shell.exe
; Versiya CI dan beriladi:  iscc /DMyAppVersion=1.2.3 dust2-klient.iss

#ifndef MyAppVersion
  #define MyAppVersion "0.1.8"
#endif

#define MyAppName "DUST2 Klient"
#define MyPublisher "DUST2 GAMEZONE"
#define ServiceName "Dust2Agent"
#define ServiceExe "Dust2Agent.Service.exe"
#define ShellExe "Dust2Agent.Shell.exe"

[Setup]
AppId={{8C1D5E2F-7A43-4B96-9C10-2D6F3B8A5E41}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyPublisher}
AppCopyright=Copyright (c) 2026 {#MyPublisher}
DefaultDirName={autopf}\DUST2 Klient
DefaultGroupName=DUST2
DisableProgramGroupPage=yes
DisableDirPage=no
UninstallDisplayName={#MyAppName} {#MyAppVersion}
OutputDir=Output
OutputBaseFilename=DUST2-Klient-Setup-{#MyAppVersion}
Compression=lzma2/max
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin
WizardStyle=modern

[Languages]
Name: "uz"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\publish\service\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\publish\shell\*";   DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Dirs]
; Jurnal va seans holati barcha foydalanuvchilarga ochiq papkada saqlanadi
Name: "{commonappdata}\DUST2"; Permissions: users-modify
Name: "{commonappdata}\DUST2\logs"; Permissions: users-modify

[Icons]
; Klient to'xtatilganda qayta ishga tushirish uchun
Name: "{autoprograms}\DUST2 klient"; Filename: "{app}\{#ShellExe}"; \
  Comment: "DUST2 klient ekranini ochish"
Name: "{autodesktop}\DUST2 klient"; Filename: "{app}\{#ShellExe}"; \
  Comment: "DUST2 klient ekranini ochish"

[Run]
; Eski xizmat qolgan bo'lsa — to'xtatib o'chiramiz
Filename: "{sys}\sc.exe"; Parameters: "stop {#ServiceName}"; Flags: runhidden
Filename: "{sys}\sc.exe"; Parameters: "delete {#ServiceName}"; Flags: runhidden
; Xizmatni ro'yxatdan o'tkazish — kompyuter yonishi bilan ishga tushadi
Filename: "{sys}\sc.exe"; \
  Parameters: "create {#ServiceName} binPath= ""{app}\{#ServiceExe}"" start= auto DisplayName= ""DUST2 klient agent"""; \
  Flags: runhidden; StatusMsg: "Xizmat ro'yxatdan o'tkazilmoqda…"
Filename: "{sys}\sc.exe"; \
  Parameters: "description {#ServiceName} ""DUST2 GAMEZONE klient agenti: seans taymeri, qulf ekrani va admin bilan aloqa."""; \
  Flags: runhidden
; Yiqilsa o'zi qayta ishga tushsin
Filename: "{sys}\sc.exe"; \
  Parameters: "failure {#ServiceName} reset= 86400 actions= restart/5000/restart/5000/restart/20000"; \
  Flags: runhidden
; Windows Firewall: agent admin kompyuterga o'zi ulanadi
Filename: "{sys}\netsh.exe"; \
  Parameters: "advfirewall firewall add rule name=""DUST2 klient agent"" dir=out action=allow program=""{app}\{#ServiceExe}"" enable=yes profile=any"; \
  Flags: runhidden; StatusMsg: "Windows Firewall qoidasi qo'shilmoqda…"
Filename: "{sys}\sc.exe"; Parameters: "start {#ServiceName}"; Flags: runhidden; StatusMsg: "Xizmat ishga tushirilmoqda…"
; Ulanish oynasi darhol ochilsin. Xizmat ham qobiqni ochadi, lekin u kechikishi yoki
; ishga tushmasligi mumkin — bunda foydalanuvchi baribir IP va kompyuter nomini kirita oladi.
; Qobiqda yagona nusxa qulfi bor, shuning uchun ikkinchi nusxa ochilmaydi.
Filename: "{app}\{#ShellExe}"; Flags: nowait postinstall skipifsilent runasoriginaluser; \
  StatusMsg: "Ulanish oynasi ochilmoqda…"

[UninstallRun]
Filename: "{sys}\taskkill.exe"; Parameters: "/F /IM {#ShellExe}"; Flags: runhidden; RunOnceId: "KillShell"
Filename: "{sys}\sc.exe"; Parameters: "stop {#ServiceName}"; Flags: runhidden; RunOnceId: "StopSvc"
Filename: "{sys}\sc.exe"; Parameters: "delete {#ServiceName}"; Flags: runhidden; RunOnceId: "DelSvc"
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall delete rule name=""DUST2 klient agent"""; Flags: runhidden; RunOnceId: "DelFw"

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
; Sozlamalar, token va jurnallar — faqat foydalanuvchi rozi bo'lsa (kodda so'raladi)

[Code]
{ Yangilashda eski xizmat faylni ushlab turmasligi uchun avval to'xtatiladi }
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM {#ShellExe}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Exec(ExpandConstant('{sys}\sc.exe'), 'stop {#ServiceName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Sleep(2000);
  Result := '';
end;

{ O'chirishda: Task Manager siyosatini tiklash, so'ng sozlamalarni tozalashni so'rash }
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DataDir: String;
begin
  if CurUninstallStep = usUninstall then
  begin
    { Qulf paytida o'chirilgan bo'lsa, siyosat qolib ketmasin }
    RegDeleteValue(HKEY_CURRENT_USER,
      'Software\Microsoft\Windows\CurrentVersion\Policies\System', 'DisableTaskMgr');
  end;

  if CurUninstallStep = usPostUninstall then
  begin
    DataDir := ExpandConstant('{commonappdata}\DUST2');
    if DirExists(DataDir) then
    begin
      if MsgBox('Sozlamalar, ulanish kaliti va jurnallar ham o''chirilsinmi?' + #13#10 +
                DataDir + #13#10#13#10 +
                'Keyinroq qayta o''rnatmoqchi bo''lsangiz, "Yo''q" ni tanlang — ' +
                'kompyuter qaytadan juftlanmaydi.',
                mbConfirmation, MB_YESNO) = IDYES then
      begin
        DelTree(DataDir, True, True, True);
      end;
    end;
  end;
end;
