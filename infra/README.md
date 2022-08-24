# ThingsAPI

## Infrastructure as Code using Azure CLI 

### Initialise variables

```
RG="Thingz-rg"
LOCATION="uksouth"
PLANNAME="appserviceplan"
APPNAME="thingzapi"
STORAGENAME="thingzstorage"

```

# create a RG - if needed

```
az group create -g $RG -l $LOCATION  -o table 

```

# create a AppService Plan - if needed

```
az appservice plan create -g $RG  \
  --name $PLANNAME \
  --is-linux \
  --number-of-workers 1 \
  --sku B1

```
   
# create storage account and get connection string 

```
az storage account create -n $STORAGENAME -g $RG -l $LOCATION --sku Standard_LRS

STORAGEKEY=$(az storage account keys list -n $STORAGENAME -g $RG  -o tsv --query "[0].value" )

printf -v STORAGECS "DefaultEndpointsProtocol=https;AccountName=$STORAGENAME;AccountKey=$STORAGEKEY;EndpointSuffix=core.windows.net" 

```

# create a WebApp and configure

```
az webapp create -g $RG -p $PLANNAME -n $APPNAME --runtime DOTNETCORE:6.0

az webapp stop -g $RG -n $APPNAME

az webapp config appsettings set -g $RG -n $APPNAME --settings \
 AdminPW="????????"

az webapp config connection-string set -g $RG -n $APPNAME -t custom --settings \
  ThingsDbConnectionString=$STORAGECS


az webapp config container set -g $RG -n $APPNAME \
  --docker-custom-image-name https://ghcr.io/markharrison/thingsapi:latest \
  --docker-registry-server-url https://ghcr.io \
  --docker-registry-server-user markharrison \
  --docker-registry-server-password ????

az webapp start -g $RG -n $APPNAME

```

# Intialise - dummy data 

```
curl -X DELETE https://$APPNAME.azurewebsites.net/api/Things

curl -H "Content-Type:application/json" -X PUT -d '{"thingid":1,"name":"Vicarage Rd","latitude": 51.649836,"longitude":-0.401486,"image": "https://placekitten.com/400/300","text": "Donec auctor magna sed nisi scelerisque malesuada","status":"red"}'  https://$APPNAME.azurewebsites.net/api/Things/1

curl -H "Content-Type:application/json" -X PUT -d '{"thingid":3,"name":"Liberty","latitude": 51.6422,"longitude":-3.9351,"status":"green"}'  https://$APPNAME.azurewebsites.net/api/Things/2

curl -H "Content-Type:application/json" -X PUT -d '{"thingid":3,"name":"St Marys","latitude": 50.905833,"longitude":-1.391111,"status":"green"}'  https://$APPNAME.azurewebsites.net/api/Things/3

curl -H "Content-Type:application/json" -X PUT -d '{"thingid":4,"name":"Madejski","latitude": 51.422222,"longitude":-0.982778,"status":"green"}'  https://$APPNAME.azurewebsites.net/api/Things/4

curl -H "Content-Type:application/json" -X PUT -d '{"thingid":5,"name":"Villa Park","latitude": 52.509167,"longitude":-1.884722,"status":"green"}'  https://$APPNAME.azurewebsites.net/api/Things/5

```
