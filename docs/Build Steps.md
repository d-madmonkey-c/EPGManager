wget <https://dot.net/v1/dotnet-install.sh> -O dotnet-install.sh
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
sudo ./dotnet-install.sh --channel 10.0 --runtime aspnetcore --install-dir /usr/share/dotnet
sudo nano /etc/profile.d/dotnet.sh
	export DOTNET_ROOT=/usr/share/dotnet
	export PATH=$PATH:/usr/share/dotnet
source /etc/profile.d/dotnet.sh

sudo ln -s /opt/epgmanager/epgmanager.service /etc/systemd/system/epgmanager.service


dotnet publish -c Release -r linux-arm64 --self-contained false
rm -Path "EPGManager\bin\Release\net10.0\linux-arm64\publish" -Include "*.br","*.gz" -Recurse -Force
scp -i C:\Users\david\.ssh\MonkeyPlex_dcutler -r EPGManager\bin\Release\net10.0\linux-arm64\publish dcutler@monkeyplex:/home/dcutler/upload/
#sudo rm -rf upload/publish/**/*.br
#sudo rm -rf upload/publish/**/*.gz
sudo systemctl stop epgmanager
sudo cp -rfv upload/publish/* /opt/epgmanager/
sudo systemctl daemon-reload
sudo systemctl restart epgmanager
#sudo systemctl status epgmanager
sudo journalctl -u epgmanager -f -n 100
