/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Component } from '@angular/core';

@Component({
  selector: 'app-types-of-vehicles',
  templateUrl: './types-of-vehicles.component.html',
  styleUrls: ['./types-of-vehicles.component.css']
})
export class TypesOfVehiclesComponent {
  types = [
    'types.list.car',
    'types.list.truck',
    'types.list.motorcycle',
    'types.list.trailer'
  ];
}

