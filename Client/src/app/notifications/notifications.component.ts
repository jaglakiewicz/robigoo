/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Component } from '@angular/core';

interface NotificationItem {
  titleKey: string;
  bodyKey: string;
  timeKey: string;
}

@Component({
  selector: 'app-notifications',
  templateUrl: './notifications.component.html',
  styleUrls: ['./notifications.component.css']
})
export class NotificationsComponent {
  notifications: NotificationItem[] = [
    { titleKey: 'notifications.items.maintenance.title', bodyKey: 'notifications.items.maintenance.body', timeKey: 'notifications.times.minutes5' },
    { titleKey: 'notifications.items.newAssignments.title', bodyKey: 'notifications.items.newAssignments.body', timeKey: 'notifications.times.hour1' },
    { titleKey: 'notifications.items.backup.title', bodyKey: 'notifications.items.backup.body', timeKey: 'notifications.times.todayMorning' }
  ];
}

