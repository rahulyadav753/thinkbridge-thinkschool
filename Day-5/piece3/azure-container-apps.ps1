az group create -n thinkschool-rg -l centralindia 
az containerapp env create -n thinkschool-env -g thinkschool-rg -l centralindia 
az containerapp env show -n thinkschool-env -g thinkschool-rg -o json