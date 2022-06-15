echo "Deploy script v1.0"
echo "--------------Stopping Service--------------"
sudo systemctl stop kestrel-TrelloScriptServer.service
echo "--------------Pulling--------------"
sudo git pull
echo "--------------Building--------------"
sudo dotnet publish --configuration Release
echo "--------------Copying files--------------"
sudo 'cp' -rf /home/tektek2000/TrelloScript/TrelloScriptServer/bin/Release/net6.0/publish/* /var/www/TrelloScriptServer/
echo "--------------Starting service--------------"
sudo systemctl start kestrel-TrelloScriptServer.service
echo "All done"