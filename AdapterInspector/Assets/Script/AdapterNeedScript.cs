using System;
using UnityEngine;
using System.Collections;

public class NameAttribute : Attribute
{
    public string Name { get; set; }

    public NameAttribute(string name)
    {
        Name = name;
    }
}