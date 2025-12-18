/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Component } from '@angular/core';

interface MarkRow {
  number: string;
  vehicle: string;
  issued: string;
  expires: string;
}

@Component({
  selector: 'app-inspection-marks',
  templateUrl: './inspection-marks.component.html',
  styleUrls: ['./inspection-marks.component.css']
})
export class InspectionMarksComponent {
  marks: MarkRow[] = [
    { number: 'ZK-2024-0012', vehicle: 'SK 12345', issued: '2024-01-12', expires: '2025-01-12' },
    { number: 'ZK-2024-0026', vehicle: 'RZ 90811', issued: '2024-02-04', expires: '2025-02-04' },
    { number: 'UA-2024-1108', vehicle: 'AE 7743 HH', issued: '2024-02-18', expires: '2025-02-18' }
  ];
}

