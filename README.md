# ACIAzFunction

POST (http://localhost:7131/api/ACI_HttpStart)

```json
{
    "ContainerGroup":{
        "subscriptionId":"e4fc0399-4ecf-4e03-b54e-27ab303b2947",
        "rgName":"MinaAM-DEMO",
        "name":"aci-bhp-demo014",
        "acrServer":"arianacr001.azurecr.io",
        "acrUserName":"arianacr001",
        "acrPassword":"<ACR password>",
        "acrImageName":"arianacr001.azurecr.io/bhp-demo/ge:latest",
        "startCommandLine":null
    }
}

```