AI Carriers
==========

A .NET port of the [AI Carriers utility by Lamont Clark](http://web.archive.org/web/20100802075258/http://lc0277.nerim.net/wiki/index.php?software).

Original description:

> **AICarriers**: This is a small software that allows you to place and control single ships or complete naval fleets in Flight Simulator X. Doesn't need complex edit of traffic files or mission files. Just add carriers when you are in free flight, anywhere you want.

This version targets the [.NET Framework 4.0 Client Profile](http://www.microsoft.com/en-us/download/details.aspx?id=17113) and supports Microsoft Flight Simulator X: Acceleration and SP2, Microsoft ESP, Lockheed Martin Prepar3D and Prepar3D v2, and Dovetail Games Microsoft Flight Simulator X: Steam Edition.

Usage
---

This version of AI Carriers has re-implemented all functionality of Lamont Clark's original Java release.  All cfg files and entries from the original should work with this version.

Simply launch the executable while Flight Simulator is running in order to connect to the simulator.  Access the in-sim menu by either using the keyboard shortcut (Shift+J) or the add-ons menu inside of Flight Simulator.

Here's a sample exe.xml file to launch AI Carriers with Flight Simulator:

    <SimBase.Document Type="Launch" version="1,0">
      <Descr>Launch</Descr>
      <Filename>exe.xml</Filename>
      <Disabled>False</Disabled>
      <Launch.ManualLoad>False</Launch.ManualLoad>
      <Launch.Addon>
        <Name>AICarriers</Name>
        <Disabled>False</Disabled>
        <ManualLoad>False</ManualLoad>
        <Path>REPLACE WITH PATH TO AICarriers.exe</Path>
      </Launch.Addon>
    </SimBase.Document>

Acknowledgements
---
- Lamont Clark - Creating the original AI Carriers utility and being a proponent of open source software. :)
- Steven Frost - Creating the ini parsing class used in the program.
- Tim Gregson - Creating the BeatlesBlog Managed SimConnect library.
