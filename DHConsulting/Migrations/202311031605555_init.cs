namespace DHConsulting.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class init : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Cliente",
                c => new
                    {
                        IdCliente = c.Int(nullable: false, identity: true),
                        Nome = c.String(nullable: false, maxLength: 50),
                        Cognome = c.String(nullable: false, maxLength: 50),
                        DataNascita = c.DateTime(nullable: false, storeType: "date"),
                        Indirizzo = c.String(nullable: false, maxLength: 100),
                        Citta = c.String(nullable: false, maxLength: 50),
                        CF = c.String(maxLength: 16, fixedLength: true, unicode: false),
                        Piva = c.String(maxLength: 11, fixedLength: true, unicode: false),
                        Username = c.String(nullable: false, maxLength: 50),
                        Password = c.String(nullable: false, maxLength: 100),
                        Email = c.String(nullable: false, maxLength: 50),
                        Phone = c.String(nullable: false, maxLength: 20),
                    })
                .PrimaryKey(t => t.IdCliente);
            
            CreateTable(
                "dbo.Ordine",
                c => new
                    {
                        IdOrdine = c.Int(nullable: false, identity: true),
                        IdCliente = c.Int(nullable: false),
                        DataOrdine = c.DateTime(),
                        InvoicePdf = c.Binary(),
                    })
                .PrimaryKey(t => t.IdOrdine)
                .ForeignKey("dbo.Cliente", t => t.IdCliente)
                .Index(t => t.IdCliente);
            
            CreateTable(
                "dbo.Dettaglio",
                c => new
                    {
                        IdDettaglio = c.Int(nullable: false, identity: true),
                        IdOrdine = c.Int(nullable: false),
                        IdProdotto = c.Int(nullable: false),
                        Quantita = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.IdDettaglio)
                .ForeignKey("dbo.Prodotto", t => t.IdProdotto)
                .ForeignKey("dbo.Ordine", t => t.IdOrdine)
                .Index(t => t.IdOrdine)
                .Index(t => t.IdProdotto);
            
            CreateTable(
                "dbo.Prodotto",
                c => new
                    {
                        IdProdotto = c.Int(nullable: false, identity: true),
                        DescrizioneBreve = c.String(nullable: false, maxLength: 50),
                        DescrizioneLunga = c.String(nullable: false),
                        Costo = c.Decimal(nullable: false, precision: 18, scale: 2),
                        CostoScontato = c.Decimal(precision: 18, scale: 2),
                        Image = c.String(maxLength: 50),
                    })
                .PrimaryKey(t => t.IdProdotto);
            
            CreateTable(
                "dbo.Utente",
                c => new
                    {
                        IdUtente = c.Int(nullable: false, identity: true),
                        Username = c.String(nullable: false, maxLength: 50),
                        Password = c.String(nullable: false, maxLength: 100),
                        Role = c.String(maxLength: 50),
                        FailedLoginAttempts = c.Int(),
                        LockoutEndTime = c.DateTime(),
                        Confirmed = c.Boolean(nullable: false),
                        Token = c.Binary(),
                    })
                .PrimaryKey(t => t.IdUtente);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Ordine", "IdCliente", "dbo.Cliente");
            DropForeignKey("dbo.Dettaglio", "IdOrdine", "dbo.Ordine");
            DropForeignKey("dbo.Dettaglio", "IdProdotto", "dbo.Prodotto");
            DropIndex("dbo.Dettaglio", new[] { "IdProdotto" });
            DropIndex("dbo.Dettaglio", new[] { "IdOrdine" });
            DropIndex("dbo.Ordine", new[] { "IdCliente" });
            DropTable("dbo.Utente");
            DropTable("dbo.Prodotto");
            DropTable("dbo.Dettaglio");
            DropTable("dbo.Ordine");
            DropTable("dbo.Cliente");
        }
    }
}
