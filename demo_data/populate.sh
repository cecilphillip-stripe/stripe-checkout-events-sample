echo "Importing product data...."
mongoimport --db=eventsshop --collection=listings --file=/data/listings.json --jsonArray --mode=upsert --username=admin --password=admin --authenticationDatabase=admin

echo "Importing customer...."
mongoimport --db=eventsshop --collection=customers --file=/data/customers.json --jsonArray --mode=upsert --username=admin --password=admin --authenticationDatabase=admin