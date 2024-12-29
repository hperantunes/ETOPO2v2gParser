nETOPO2v2 2006 (version: ETOPO2v2g - Grid-centered registration)

Data set credit:

U.S. Department of Commerce
National Oceanic and Atmospheric Administration
National Environmental Satellite, Data, and Information Service
National Geophysical Data Center
http://www.ngdc.noaa.gov/mgg/fliers/06mgg01.html

NOTE: the ETOPO2v2 grid-centered registration grids have recently been re-worked in order
      to repair some problems.
       
ETOPO2 grids are in meters.

Complete ETOPO2v2g grids are under folder
http://www.ngdc.noaa.gov/mgg/global/relief/ETOPO2/ETOPO2v2-2006/ETOPO2v2g/
as follows:

netCDF/
ETOPO2v2g_f4_netCDF.zip - 4-byte floating pt grid in COARDS-compliant netCDF Format

GRD98/
ETOPO2v2g_i2_LSB_GRD98.zip - 2-byte integer Most Significant Byte First (bigendian) grid in GRD98 Format
ETOPO2v2g_i2_MSB_GRD98.zip - 2-byte integer Least Significant Byte First (littleendian) grid in GRD98 Format

raw-binary/
ETOPO2v2g_i2_LSB.zip - 2-byte integer grid with Least Significant Byte First (littleendian)
ETOPO2v2g_i2_MSB.zip - 2-byte integer grid with Most Significant Byte First (bigendian)
ETOPO2v2g_f4_LSB.zip - 4-byte floating pt grid with Least Significant Byte First (littleendian)
ETOPO2v2g_f4_MSB.zip - 4-byte floating pt grid with Most Significant Byte First (bigendian)

NOTE: For Grid-centered grids only, you can create custom grids with user-defined bounds
and in several different formats at: http://www.ngdc.noaa.gov/mgg/gdas/gd_designagrid.html

ETOPO2v2g Data structure and provenance:

First and foremost, ETOPO2v2g eliminates a 1-cell westward bias that was present 
in ETOPO2 as a result of alignment adjustments to match the choice of grid 
registration used in sampling GLOBE data. The "g" in the name denotes the 
grid-centered version.

ETOPO2v2g is in a grid-centered registration (pixel registered); the cell
boundaries are lines of odd minutes of latitude and longitude, and they are
centered on intersections of lines of even minutes of latitude and longitude.
This is the same as the original ETOPO2, which was also grid-registered, meaning
that cells were centered on the integer multiples of 2 minutes (even minutes) of
latitude and longitude and the cell boundaries were consequently defined by
lines of odd minutes of latitude and longitude. The first row of 10,801 (360 x
30) cells in ETOPO2 redundantly repeated the same data value, centered on the
north pole. A data row, centered on the South Pole, was not included in the
original ETOPO2, but exists in ETOPO2v2g.

Coverage of ETOPO2v2g is -90deg to +90deg in Latitude, and -180deg to +180deg in
Longitude, whereas ETOPO2 covered +90deg to -89deg58' in Latitude and -180Wdeg
to +189deg58' in Longitude. The new ETOPO2v2g, with grid centered-registration,
contains North and South Pole redundancies but has a different number of cells:
5,401 rows of data (180x30 + 1), each with 10,801 columns of data(360x30 + 1).
The extra column of data replicates the first column and is included for GMT
compatibility.


Other Forms:

  ETOPO2v2c is in a cell-centered registration (pixel registered); the cell 
  boundaries are lines of even minutes of latitude and longitude, and they are
  centered on intersections of lines of odd minutes of latitude and longitude.

  The ETOPO2v2c Cell-centered (pixel-registered) version of ETOPO2 is the
  definitive version. This ETOPO2v2g was re-interpolated using GMT. For info
  the building of ETOPO2v2c, see:
  http://www.ngdc.noaa.gov/mgg/global/relief/ETOPO2/ETOPO2v2-2006/ETOPO2v2c/ETOPO2v2c_ReadMe.txt

Availability:

ETOPO2v2 is available in its entirety on DVD:
http://www.ngdc.noaa.gov/mgg/fliers/06mgg01.html

You can create custom gridline-registered grids of ETOPO2 and other
grids with lat/lon bounds of your choosing into several formats using:
http://www.ngdc.noaa.gov/mgg/gdas/gd_designagrid.html
