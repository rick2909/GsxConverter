# GSX Python Customization (extracted from PDF)

From now on, the following chapters will be of interest to profile creators only, since the topic can be quite advanced, and the rules are very strict, and any error will cause the customization file to be rejected.

First, you need to use a good text editor. While the default Windows Notepad is ok, something more advanced like Notepad++ is suggested. Some familiarity with Python is useful, but not really required — we need to assume at least some basic knowledge of any modern programming language (Python, JavaScript, C#, etc.). In this tutorial we’ll use the default Milano Linate (LIML) airport as a test, to explain step by step how the Python customization works.

In general, you need to create an empty file with the text editor and call it `ICAOxxxxx.PY`. In this case we can call it `LIML-TEST.PY`. It’s not important the characters after the `-` (dash) to be identical to the ones in an eventual `.INI` profile; only the ICAO matters.

Minimal Python file example:
```python
msfs_mode = 1
parkings = {
    GATE : {
        None : (),
        1 : (CustomizedName("TEST Group|Gate 01"), ),
    }
}
```

This is the bare minimum Python file, which in this case is used to customize a single parking position, placing it in a new group with a custom name. The result in GSX would look like this in the customization page.

When using Python files, always keep an eye on the `Couatl.log` (you’ll have to restart Couatl at every change), because Python files are extremely sensitive to errors — even a misplaced comma will cause the file to be rejected.

We strongly suggest enabling Logging in the Troubleshooting section of the GSX Settings, so you can keep track of any errors and if your file has been loaded. Remember to always restart Couatl after every change.

If everything goes right, you are supposed to see a line in the Couatl.LOG indicating the file was loaded if you are at LIML and have `LIML.TEST.PY` in the profiles folder.

---

## Line-by-line explanation

The first line:
```python
msfs_mode = 1
```
This means the file is meant for MSFS only. If you don’t set this, the file will be loaded by FSX and P3D as well, so we usually set it as the first line since parking positions between FSX/P3D and MSFS rarely match.

The second line starts with:
```python
parkings =
```
This means you are overriding the `parkings` Python object that is already in memory and replacing it with your custom version.

Since an airport has groups and parking spots inside groups, a typical file will have a top-level dictionary of groups, and each of these groups will contain a dictionary of parking spots. If you don’t define something, it will be left at its default value.

Next:
```python
GATE : {
```
This starts the definition of a Python dictionary that is contained in the first global `parkings` object. Python dictionaries are key:value structures. The key for this dictionary is the enum that matches the standard values used in the SDK (in this example `GATE`). The value for this key is another dictionary of parking spots (keyed by parking number) and the values are the commands that define custom options for each parking position.

Second-level nested dictionary example:
```python
None : (),
1 : (CustomizedName("TEST Group|Gate 01"), ),
```
The `None` entry has a special meaning: it indicates general parameters that will be valid for all parking positions in that group, unless individually overridden. In this minimal example we only customized one position, so the nested dictionary has two entries: `None` for general options and `1` to redefine Gate 1.

The Python file then ends with two closing braces to close the two nested dictionaries.

---

## The CustomizedName command

The most important command to allow customization is:
```python
1 : (CustomizedName("TEST Group|Gate 01"), ),
```
`CustomizedName` takes one string parameter that defines a custom Group and Name for that parking position. In the example above, the gate will be listed as "Gate 01" under the "TEST Group" menu in the GSX parking selection menu.

Pay attention to the special pipe character `|` which separates the Group from the Parking name. If you don't use the pipe, the string will only change the parking name with no effect on grouping.

To add another parking in the same group:
```python
msfs_mode = 1
parkings = {
    GATE : {
        None : (),
        1 : (CustomizedName("TEST Group|Gate 01"), ),
        2 : (CustomizedName("TEST Group|Gate 02"), ),
    }
}
```
Both Gate 1 and 2 will end up under the "TEST Group" menu, named "Gate 01" and "Gate 02".

---

## Splitting positions into different groups

You can reassign parking positions to real-life terminals / concourses to make the GSX menu more usable.

Example:
```python
parkings = {
    GATE : {
        None : (),
        1 : (CustomizedName("Terminal 1 Concourse A|Gate 1"), ),
        2 : (CustomizedName("Terminal 1 Concourse A|Gate 2"), ),
        3 : (CustomizedName("Terminal 1 Concourse B|Gate 3"), ),
        4 : (CustomizedName("Terminal 1 Concourse B|Gate 4"), ),
    }
}
```
This reassigns gates 1 and 2 to "Terminal 1 Concourse A" and gates 3 and 4 to "Terminal 1 Concourse B".

---

## Merging positions from different groups into a new group

If the scenery placed gates in different group names (`GATE_A`, `GATE_B`) you can map them into a single group in GSX:

```python
parkings = {
    GATE_A : {
        None : (),
        1 : (CustomizedName("Terminal 1|Gate 1"), ),
        2 : (CustomizedName("Terminal 1|Gate 2"), ),
    },
    GATE_B : {
        None : (),
        3 : (CustomizedName("Terminal 1|Gate 3"), ),
        4 : (CustomizedName("Terminal 1|Gate 4"), ),
    }
}
```
All gates will appear under the "Terminal 1" group in the GSX menu.

---

## Using the `None` group to avoid repetition

If a general pattern fits an entire group, use the `None` key to define properties common to all parking positions of the same group.

Example where many parkings follow a pattern:
```python
parkings = {
    GATE_A : {
        None : ( CustomizedName("Terminal 1|Gate #"), ),
    },
    GATE_B : {
        None : ( CustomizedName("Terminal 2|Gate #"), ),
    }
}
```
If GATE_A had 100 positions and GATE_B had 60, you avoid repeating similar lines by using the `None` entry.

---

## Expanding parking numbers with the `#` symbol

The `#` character is used to expand a parking number. When applied to the `None` entry, it means all parking positions in that group would use that rule, generating names like "Gate 1", "Gate 2", etc.

You can still add exceptions for specific parking numbers:
```python
parkings = {
    GATE_A : {
        None : ( CustomizedName("Terminal 1|Gate #"), ),
        54 : ( CustomizedName("Terminal 1|Heavy Gate # (A380-specific)"), ),
    },
    GATE_B : {
        None : ( CustomizedName("Terminal 2|Gate #"), ),
    }
}
```
Here, Gate 54 is an exception.

---

## Expanding the suffix with the `§` symbol

The `§` symbol (Section Sign) can be used with `#` to include suffixes (A, B, etc.) that represent additional parking variants:

```python
parkings = {
    GATE_A : {
        None : ( CustomizedName("Terminal 1|Gate #§"), ),
    },
    GATE_B : {
        None : ( CustomizedName("Terminal 2|Gate #§"), ),
    }
}
```

If a gate has a suffix like `A` or `B` (i.e., `GATE_A`, `GATE_B` in the SDK), the suffix will appear after the number: "Gate 2", "Gate 2A", "Gate 2B", etc.

---

## Matching gates with no names

Some sceneries may use parking positions with numbers but no names (SDK enum `NONE`), whose internal numeric value is `0`. Use the `0` group to match those:

```python
parkings = {
    0 : {
        None : ( CustomizedName("Terminal A|Gate #§"), ),
    },
}
```
This reassigns unnamed parking spots to "Terminal A".

---

## Customized STOP positions

By default, GSX uses the Preferred Exit method to calculate the STOP position. This usually works but may not match custom ground markings on third-party airports. Python customization allows defining custom STOP positions based on airplane models and parking spots.

Custom Stop functions are decorated with `@AlternativeStopPositions`. Example:
```python
@AlternativeStopPositions
def customOffset_T1_HeavyGates(aircraftData):
    return Distance.fromMeters(10)
```
- The decorator `@AlternativeStopPositions` marks the function as a custom stop definition.
- The function input parameter (here `aircraftData`) contains data about the aircraft, supplied by GSX.
- The function must return a `Distance` (a GSX data type). Example returning 10 meters with `Distance.fromMeters(10)`.

Assign the custom function to gates as you would assign `CustomizedName`:
```python
parkings = {
    GATE_A : {
        None : ( CustomizedName("Terminal 1|Gate #§"), customOffset_T1_HeavyGates ),
    },
    GATE_B : {
        None : ( CustomizedName("Terminal 2|Gate #§"), ),
    }
}
```
Setting it in the `None` entry assigns the function to all parking spots within the group.

To assign to individual parking positions:
```python
parkings = {
    GATE_A : {
        None : (),
        1 : ( CustomizedName("Terminal 1|Gate 1"), customOffset_T1_HeavyGates ),
        2 : ( CustomizedName("Terminal 1|Gate 2"), ),
    },
    GATE_B : {
        None : (),
        3 : ( CustomizedName("Terminal 1|Gate 3"), customOffset_T1_HeavyGates ),
        4 : ( CustomizedName("Terminal 1|Gate 4"), ),
    }
}
```
Here parking 1 (Gate A) and parking 3 (Gate B) use the custom stop function; others use default STOP calculation.

Return value detail:
```python
return Distance.fromMeters(10)
```
When returning a fixed value, every airplane will stop the same distance (e.g., 10 m) ahead of the base Stop Position object. Usually a per-aircraft-type approach is more useful.

---

## Customized STOP positions by airplane type

Most common usage: return different stop distances based on aircraft type. Use `aircraftData` to check `idMajor` and `idMinor`.

Simple example:
```python
@AlternativeStopPositions
def customOffset_T1_HeavyGates(aircraftData):
    if aircraftData.idMajor == 737:
        return Distance.fromMeters(1)
    elif aircraftData.idMajor == 320:
        return Distance.fromMeters(2)
    else:
        return Distance.fromMeters()
```
- `idMajor` contains codes for common airplane types.
- The last `else` returns a Distance of 0 (default).

Variant-aware example:
```python
@AlternativeStopPositions
def customOffset_T1_HeavyGates(aircraftData):
    if aircraftData.idMajor == 737:
        if aircraftData.idMinor == 600:
            return Distance.fromMeters(-2)
        elif aircraftData.idMinor == 700:
            return Distance.fromMeters(-1)
        else:
            return Distance.fromMeters(1)
    else:
        return Distance.fromMeters()
```
This differentiates between 737 variants (737-600, -700, others).

---

## Use tables (dictionaries) for maintainable code

If many models/variants must be handled, prefer dictionaries ("tables") over long if/elif chains.

Example mapping:
```python
@AlternativeStopPositions
def customOffset_T1_Gates(aircraftData):
    myTable = {
        0: 8.05,
        737: 0,
        195: 0,
        900: 0,
        757: 3.5,
        321: 3.5,
        320: 3.5,
        777: 8.05,
        330: 8.05,
        350: 8.05,
        747: 12.05,
        767: 12.05,
        340: 12.05,
    }
    return Distance.fromMeters(myTable.get(aircraftData.idMajor))
```

Handling missing keys (avoid KeyError):
```python
return Distance.fromMeters(myTable.get(aircraftData.idMajor, 0))
```
Or use try/except:
```python
try:
    return Distance.fromMeters(myTable.get(aircraftData.idMajor))
except:
    return Distance.fromMeters(0)
```

Example with a specific table for 787 variants:
```python
@AlternativeStopPositions
def customOffset_T1_Gates(aircraftData):
    myTable = {
        0: 8.05,
        737: 0,
        195: 0,
        900: 0,
        757: 3.5,
        321: 3.5,
        320: 3.5,
        777: 8.05,
        330: 8.05,
        350: 8.05,
        747: 12.05,
        767: 12.05,
        340: 12.05,
    }
    myTable787 = {
        8: 8.05,
        9: 12.05,
        10: 12.05,
    }
    if aircraftData.idMajor == 787:
        return Distance.fromMeters(myTable787.get(aircraftData.idMajor, 0))
    else:
        return Distance.fromMeters(myTable.get(aircraftData.idMajor, 0))
```
You can define as many tables as needed and assign custom stop functions to multiple parking spots.

---

## STOP positions with lateral offsets

Usually the function returns a single longitudinal distance. To specify lateral offset as well, return a tuple `(longitudinal, lateral)` — both values as `Distance` types or numeric values that will be converted.

Example lateral offset for ERJ-190:
```python
if aircraftData.idMinor == 190:
    return ( Distance.fromMeters(0), Distance.fromMeters(-5.0) )
```
This parks the ERJ-190 5.0 meters to the left of the parking center line.

Table example with a lateral offset entry:
```python
distances = {
    0: 0,
    170: 1.0,
    175: 2.0,
    190: (3.0, -5.0),
}
```
An aircraft with code 190 will be parked 3.0 meters ahead and 5.0 meters left of the center line.

---

## IDs used by VGDS (VGDS text mapping)

The table below lists airplane major/minor IDs and their VGDS text mapping used by GSX. (This is the original mapping excerpt.)

- Airbus A220-100 — idMajor 221 — A221  
- Airbus A220-300 — 223 — A223  
- Airbus A300 — 300 — A300  
- Airbus A310 — 310 — A310  
- Airbus A318 — 318 — A318  
- Airbus A319 — 319 — A319  
- Airbus A320 — 320 — A320  
- Airbus A321 — 321 — A321  
- Airbus A330 — 330 — A330  
- Airbus A340 — 340 — A340  
- Airbus A350 — 350 — A350  
- Boeing 707 — 707 — B707  
- Boeing 717 — 717 — B717  
- Boeing 727 — 727 — B727  
- Boeing 737 — 737 — B737  
- Boeing 747 — 747 — B747  
- Boeing 757 — 757 — B757  
- Boeing 767 — 767 — B767  
- Boeing 777 — 777 — B777  
- Boeing 787 — 787 — B787  
- CRJ-200 — 200 — CJ200  
- CRJ-700 — 700 — CJ700  
- CRJ-900 — 900 — CJ900  
- CRJ-1000 — 1000 — CRJX  
- Douglas DC-6 — 6 — DC6  
- Douglas DC-7 — 7 — DC7  
- Douglas DC-8 — 8 — DC8  
- Douglas DC-9 — 9 — DC9  
- Douglas DC-10 — 10 — DC10  
- McDonnell-Douglas MD82 — 82 — MD82  
- McDonnell-Douglas MD83 — 83 — MD83  
- McDonnell-Douglas MD87 — 87 — MD87  
- McDonnell-Douglas MD90 — 90 — MD90  
- McDonnell-Douglas MD11 — 11 — MD11  
- BAe-146 — 146 — BA146  
- Embraer ERJ-135 — 135 — EJ135  
- Embraer ERJ-140 — 140 — EJ140  
- Embraer ERJ-145 — 145 — EJ145  
- Embraer ERJ-170 — 170 — EJ170  
- Embraer ERJ-175 — 175 — E175  
- Embraer ERJ-190 — 190 — E190  
- Embraer ERJ-195 — 195 — E195  
- ATR-42 — 42 — AT42  
- ATR-72 — 72 — AT72  
- Concorde — 20000 — CONC  
- De Havilland Canada DHC-8 — 1008 — DASH8  
- De Havilland Canada DHC-3 — 300 — DHC3  
- De Havilland Canada DHC-6 — 600 — DHC6  
- Fokker F-28 — 28 — F28

---

## ICAO type and Airplane Groups (GSX 2.9.2 update — Feb 2024)

Two new features were added to the Python customization: the ability to check the aircraft ICAO type and the aircraft “group”. You can now query the `aircraftData` object with:

- `aircraftData.icaoTypeDesignator` (string) — the ICAO model type as found in the airplane profile or `aircraft.cfg`.
- `aircraftData.aircraftGroup` (string) — the associated group according to GSX internal database (331 airplane types). If a code is not found it will return `"Unknown"`.

Because these properties are strings, dictionary keys for lookups must be quoted.

Example checking ICAO:
```python
TableIcao = {
    "B738" : -2.0,
    "B744" : 5.0,
    "A20N" : 1.0,
}
return Distance.fromMeters(TableIcao.get(aircraftData.icaoTypeDesignator, 0))
```
If the ICAO is not found, the default `0` is returned.

Example checking group:
```python
TableGroup = {
    "ARC-C" : -2.0,
    "ARC-D" : 2.0,
    "ARC-F" : 5.0,
}
return Distance.fromMeters(TableGroup.get(aircraftData.aircraftGroup, 0))
```

If you need a non-zero default for unknown groups:
```python
TableGroup = {
    "ARC-C" : -2.0,
    "ARC-D" : 2.0,
    "ARC-F" : 5.0,
    "Unknown" : 2.4,
}
```

---