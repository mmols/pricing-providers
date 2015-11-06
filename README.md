Custom Pricing Providers
===========

This fork of EPiServer Quicksilver highlights the abilty to customize pricing within the EPiServer Framework. It includes a sample implementation of IPriceService and IPriceDetailService that demonstrate the ability to:

* Change EPiServer's logic on which price it chooses when selecting prices for display and business processing
* Change where EPiServer stores prices

Read http://world.episerver.com/documentation/Items/Developers-Guide/EPiServer-Commerce/9/Pricing/Pricing/ prior to working with this code.

**Warning: This code is not intended for production usage and has not been performance tested. It is intended solely as a proof of concept.**

The following two classes are the main drivers, and are wired in via StructureMap at initialization time

InMemoryPriceDetailService : IPriceDetailService
---------------------
This class demonstrates that EPiServer can utilize an alternate data store for pricing by implementing a custom IPriceDetailService. In this case, the alternate data store is the InMemoryPriceDatabase class, which stores pricing in a dictionary in memory. This database is seeded at startup with the following prices for all variants:

* Price: $40 - Minimum Quantity: 0 - Sale Type: All Customers
* Price: $37 - Minimum Quantity: 02 - Sale Type: All Customers
* Price: $37 - Minimum Quantity: 02 - Sale Type: User for username "admin"

MyPriceService : IPriceService
-----------------------

This class demonstrates that you can change how EPiServer chooses which price for display and business processing. This implementation implements the standard rules that EPiServer's standard implementation does, but also extends it to implement a custom Price type called "Price Override", which will be prioritized over other prices, regardless of their price types.

See https://github.com/episerver/Quicksilver for Setup Instructions
