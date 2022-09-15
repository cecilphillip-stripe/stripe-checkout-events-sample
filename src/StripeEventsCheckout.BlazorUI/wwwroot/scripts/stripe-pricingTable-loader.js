let loaded = false;

export function loadPricingTableScript() {
    let stripePTableJS = "https://js.stripe.com/v3/pricing-table.js";
    if(!loaded) {
        
        // create JS library script element
        let script = document.createElement("script");
        script.name = "stripe-table";
        script.src = stripePTableJS;
        script.type = "text/javascript";
        
        // loading successful 
        script.onload = function () {
           loaded = true;
        };

        // loading fails
        script.onerror = function () {
            loaded = false;
        }

        // add to head
        let header = document.querySelector('head')
        header.append(script)
    }
}





