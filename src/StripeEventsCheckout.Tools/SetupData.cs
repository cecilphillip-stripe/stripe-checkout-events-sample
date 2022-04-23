
public static class DemoData
{
    public static IEnumerable<DemoRecord> Retrieve()
    {
        var data = new DemoRecord[] {
            new("Stephen","Holmes","FoodieLand Night Market","foodland@example.com",
                new("FoodieLand Night Market - Berkeley | October 8-10",
                    5500,
                    "https://img.evbuc.com/https%3A%2F%2Fcdn.evbuc.com%2Fimages%2F137700905%2F285623250502%2F1%2Foriginal.20210604-004626?w=800&auto=format%2Ccompress&q=75&sharp=10&rect=0%2C0%2C2160%2C1080&s=cea8d8fa42dee21c5740c5de763915a4"
                )
            ),

            new("Rachel","Wilkins","Craft Hospitality","craft_hospitality@example.com",
                new("San Francisco Coffee Festival 2021",
                    6900,
                    "https://img.evbuc.com/https%3A%2F%2Fcdn.evbuc.com%2Fimages%2F108330301%2F35694333470%2F1%2Foriginal.20200811-200320?w=800&auto=format%2Ccompress&q=75&sharp=10&rect=0%2C150%2C1348%2C674&s=c5f8ca6e7bd6900fae94dbc098b932a7"
                )
            ),

            new("Angela","Bruton","Shipyard Trust for the Arts","shipyardarts@example.com",
                new("Shipyard Open Studios 2021",
                    1400,
                    "https://img.evbuc.com/https%3A%2F%2Fcdn.evbuc.com%2Fimages%2F141827421%2F165638753394%2F1%2Foriginal.20210716-073309?w=800&auto=format%2Ccompress&q=75&sharp=10&rect=0%2C22%2C1920%2C960&s=13b4f039611eb487131e34d027cc8a5c"
                )
            ),

            new("Jane","Diaz","Young Art Records","tokimonsta@example.com",
                new("TOKiMONSTA presented by Young Art Records",
                    3500,
                    "https://img.evbuc.com/https%3A%2F%2Fcdn.evbuc.com%2Fimages%2F144134661%2F481588047555%2F1%2Foriginal.20210810-174543?w=800&auto=format%2Ccompress&q=75&sharp=10&rect=0%2C60%2C1920%2C960&s=54e54d81acde2a8b723576f016fc19ef"
                )
            ),

            new("Paul","Elliot","Holly Shaw and the Performers & Creators Lab","hollyshaw@example.com",
                new("The Comedy Edge: Stand-Up on the Waterfront",
                    2000,
                    "https://img.evbuc.com/https%3A%2F%2Fcdn.evbuc.com%2Fimages%2F116415537%2F38806056114%2F1%2Foriginal.20201031-204217?w=800&auto=format%2Ccompress&q=75&sharp=10&rect=0%2C0%2C2160%2C1080&s=1ab11e65f9bf740b9411d67ccdde50ad"
                )
            ),


            new("Curtis","Hall","District Six San Francisco","district6@example.com",
                new ("The Night Market",
                    2500,
                    "https://img.evbuc.com/https%3A%2F%2Fcdn.evbuc.com%2Fimages%2F143639347%2F171723859036%2F1%2Foriginal.20210805-003841?w=800&auto=format%2Ccompress&q=75&sharp=10&rect=178%2C22%2C1840%2C920&s=24792d3688d29247609481307ac2049d"
                )
            ),

            new("Arthur","Jenkins","Nor Cal Ski and Snowboard Festivals","norcalski@example.com",
                new("2021 San Francisco Ski & Snowboard Festival",
                    5000,
                    "https://img.evbuc.com/https%3A%2F%2Fcdn.evbuc.com%2Fimages%2F145722117%2F23330292812%2F1%2Foriginal.20210826-185332?w=800&auto=format%2Ccompress&q=75&sharp=10&rect=0%2C0%2C2160%2C1080&s=fb620a4c45c97a8a29830e8f8c356815"
                )
            ),

            new("Sally","Rock","Noise Pop","noisepop@example.com",
                new(
                    "Noise Pop 20th Street Block Party",
                    500,
                    "https://img.evbuc.com/https%3A%2F%2Fcdn.evbuc.com%2Fimages%2F159520329%2F578928699583%2F1%2Foriginal.20211001-140233?w=800&auto=format%2Ccompress&q=75&sharp=10&rect=0%2C25%2C1500%2C750&s=4c9f891cd9a66959bc18b523f98b2815"
                )
            ),

            new("Jenny","Fields","Sundaze San Francisco","sundaze@example.com",
                new(
                    "Sundaze Brunch & Marketplace",
                    17500,
                    "https://img.evbuc.com/https%3A%2F%2Fcdn.evbuc.com%2Fimages%2F143641099%2F171723859036%2F1%2Foriginal.20210805-010723?w=800&auto=format%2Ccompress&q=75&sharp=10&rect=61%2C0%2C2004%2C1002&s=58fcb259bb043d8239cc0c14aa04f89d"
                )
            )
        };
        return data;
    }
}


public record DemoRecord(
    string FirstName,
    string LastName,
    string Company,
    string Email,
    DemoProduct Product
);

public record DemoProduct(string Name, long Price, string Image);