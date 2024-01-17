SHELL=/bin/sh
INSTALL=install
INSTALL_DATA=$(INSTALL) -m 644
bindir=$(DESTDIR)/opt/epmgr

.NOTPARALLEL: default
default: restore build

clean:
	dotnet clean epmgr/epmgr.csproj
	rm -rf publish

restore:
	dotnet restore epmgr/epmgr.csproj

build:
	dotnet publish -o publish/epmgr -c Release --runtime linux-x64 --self-contained false epmgr/epmgr.csproj

install:
	$(INSTALL) -d $(bindir)
	$(INSTALL_DATA) publish/epmgr/*.dll $(bindir)
	$(INSTALL) publish/epmgr/epmgr $(bindir)
	$(INSTALL_DATA) publish/epmgr/epmgr.*.json $(bindir)
	$(INSTALL_DATA) publish/epmgr/appsettings.json $(bindir)
	cp -ru publish/epmgr/wwwroot $(bindir)/wwwroot