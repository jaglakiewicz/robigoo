import { Component, OnInit, OnDestroy } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { AuthService } from '../services/auth.service';
import { UserService, CreateUserRequest, UserDTO } from '../services/user.service';
import { NotificationService } from '../services/notification.service';
import { SVG_ICONS } from '../shared/svg-icons';

@Component({
  selector: 'app-settings',
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.css']
})
export class SettingsComponent implements OnInit, OnDestroy {
  activeTab = 0;
  private editUserListener: any;
  SVG_ICONS = SVG_ICONS; // Make SVG_ICONS available in template

  tabs = [
    { label: 'Ustawienia programu' },
    { label: 'Ustawienia użytkownika' },
    { label: 'Zarządzanie użytkownikami' }
  ];

  // Program Settings (placeholder for future use)
  programSettings = {
    defaultPrinter: 'pdf',
    dateFormat: 'dd.MM.yyyy',
    backupPath: 'C:\\Backups'
  };

  // User Settings
  userSettings = {
    login: '',
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    permissionNumber: '',
    language: 'pl',
    theme: 'light',
    avatar: null as string | null
  };

  // Password change
  passwordForm = {
    oldPassword: '',
    newPassword: '',
    confirmPassword: ''
  };
  changePasswordError = '';
  changePasswordSuccess = false;
  changingPassword = false;
  showPasswordForm = false;

  // Settings save messages
  userSettingsError = '';
  userSettingsSuccess = false;
  savingSettings = false;
  avatarError = '';
  avatarSuccess = false;

  // Admin Settings
  adminSettings = {
    enableLogs: true,
    logLevel: 'info',
    maxUsers: 100
  };

  // User Management
  users: UserDTO[] = [];
  showCreateUserForm = false;
  newUserForm = {
    login: '',
    password: '',
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    permissionNumber: '',
    role: 'user'
  };
  creatingUser = false;

  // Password confirmation dialog
  showPasswordConfirmDialog = false;
  confirmDialogAction: 'delete' | 'create' = 'delete';
  userToConfirm: UserDTO | null = null;
  confirmDialogPassword = '';
  confirmDialogError = '';
  confirmingAction = false;
  deletingUserId: number | null = null;

  // Role change
  updatingRoleUserId: number | null = null;

  constructor(
    private authService: AuthService,
    private userService: UserService,
    private notificationService: NotificationService,
    private sanitizer: DomSanitizer
  ) {}

  ngOnInit(): void {
    try {
      console.log('[Settings] Component initialized');
      
      // Listener for edit user settings event
      this.editUserListener = (event: any) => {
        const user = event.detail;
        console.log('[Settings] Edit user event:', user);
        this.activeTab = 1;
      };
      window.addEventListener('editUserEvent', this.editUserListener);

      this.loadUserSettings();
      this.loadUsers();
    } catch (error) {
      console.error('[Settings] Error during initialization:', error);
    }
  }

  ngOnDestroy(): void {
    window.removeEventListener('editUserEvent', this.editUserListener);
  }

  loadUserSettings(): void {
    const currentUser = this.authService.getCurrentUser();
    if (currentUser) {
      this.userSettings.login = currentUser.login;
      this.userSettings.firstName = currentUser.firstName;
      this.userSettings.lastName = currentUser.lastName;
      this.userSettings.email = currentUser.email;
      this.userSettings.phone = currentUser.phone;
      this.userSettings.permissionNumber = currentUser.permissionNumber;
      if (currentUser.avatarBase64) {
        this.userSettings.avatar = 'data:image/png;base64,' + currentUser.avatarBase64;
      }
    }
  }

  loadUsers(): void {
    this.userService.getUsers().subscribe(
      (data: any) => {
        if (data && Array.isArray(data)) {
          this.users = data;
        } else {
          console.error('[Settings] Unexpected response format:', data);
        }
      },
      (error: any) => {
        console.error('[Settings] Error loading users:', error);
      }
    );
  }

  saveUserSettings(): void {
    this.userSettingsError = '';
    this.userSettingsSuccess = false;
    this.savingSettings = true;

    const currentUser = this.authService.getCurrentUser();
    const isAdmin = currentUser && currentUser.role === 'admin';

    // Regular users can only edit: email, phone, language, theme
    // Admins can also edit: firstName, lastName, permissionNumber
    const updateData: any = {
      email: this.userSettings.email,
      phone: this.userSettings.phone,
      language: this.userSettings.language,
      theme: this.userSettings.theme
    };

    // Only admins can modify firstName, lastName, and permissionNumber
    if (isAdmin) {
      updateData.firstName = this.userSettings.firstName;
      updateData.lastName = this.userSettings.lastName;
      updateData.permissionNumber = this.userSettings.permissionNumber;
    }

    this.userService.updateProfile(updateData).subscribe(
      (response) => {
        console.log('[Settings] Profile updated:', response);
        this.notificationService.success('Ustawienia zostały pomyślnie zapisane!');
        this.savingSettings = false;

        // Update current user in auth service
        const currentUser = this.authService.getCurrentUser();
        if (currentUser) {
          // Always update these fields
          currentUser.email = this.userSettings.email;
          currentUser.phone = this.userSettings.phone;
          
          // Update admin-only fields if user is admin
          if (isAdmin) {
            currentUser.firstName = this.userSettings.firstName;
            currentUser.lastName = this.userSettings.lastName;
            currentUser.permissionNumber = this.userSettings.permissionNumber;
          }
          
          localStorage.setItem('currentUser', JSON.stringify(currentUser));
        }
      },
      (error: any) => {
        console.error('[Settings] Error saving settings:', error);
        const errorMsg = error.error?.message || 'Błąd podczas zapisywania ustawień';
        this.notificationService.error(errorMsg);
        this.savingSettings = false;
      }
    );
  }

  togglePasswordForm(): void {
    this.showPasswordForm = !this.showPasswordForm;
    if (!this.showPasswordForm) {
      // Clear form when closing
      this.passwordForm = { oldPassword: '', newPassword: '', confirmPassword: '' };
      this.changePasswordError = '';
    }
  }

  changePassword(): void {
    if (this.passwordForm.newPassword !== this.passwordForm.confirmPassword) {
      this.notificationService.error('Hasła nie pasują do siebie');
      return;
    }

    if (this.passwordForm.newPassword.length < 6) {
      this.notificationService.error('Hasło musi mieć co najmniej 6 znaków');
      return;
    }

    this.changingPassword = true;

    this.userService.changePassword({
      oldPassword: this.passwordForm.oldPassword,
      newPassword: this.passwordForm.newPassword
    }).subscribe(
      (response) => {
        console.log('[Settings] Password changed:', response);
        this.notificationService.success('Hasło zostało zmienione pomyślnie!');
        this.changingPassword = false;
        this.passwordForm = { oldPassword: '', newPassword: '', confirmPassword: '' };
        this.showPasswordForm = false;
      },
      (error: any) => {
        console.error('[Settings] Error changing password:', error);
        const errorMsg = error.error?.message || 'Błąd podczas zmiany hasła';
        this.notificationService.error(errorMsg);
        this.changingPassword = false;
      }
    );
  }

  onAvatarSelected(event: any): void {
    const file: File = event.target.files[0];

    if (file) {
      // Validate file size (max 2MB)
      if (file.size > 2097152) { // 2MB
        this.avatarError = 'Plik jest za duży (maksymalnie 2MB)';
        return;
      }

      // Validate file type
      if (!file.type.startsWith('image/')) {
        this.avatarError = 'Plik musi być obrazem (JPG, PNG, GIF)';
        return;
      }

      const reader = new FileReader();
      reader.onload = (e: any) => {
        this.userService.uploadAvatar(file).subscribe(
          (response) => {
            console.log('[Settings] Avatar uploaded:', response);
            // Reload current user to get updated avatar
            this.loadUserSettings();
            this.avatarSuccess = true;
            this.avatarError = '';
            setTimeout(() => { this.avatarSuccess = false; }, 3000);
          },
          (error: any) => {
            console.error('[Settings] Error uploading avatar:', error);
            this.avatarError = error.error?.message || 'Błąd podczas przesyłania avatara';
          }
        );
      };
      reader.readAsDataURL(file);
    }
  }

  changeTab(tabIndex: number): void {
    console.log('[Settings] Switching to tab:', tabIndex);
    
    // Prevent non-admin users from accessing admin tab (tab 2)
    const currentUser = this.authService.getCurrentUser();
    if (tabIndex === 2 && (!currentUser || currentUser.role !== 'admin')) {
      console.warn('[Settings] User does not have permission to access admin tab');
      return;
    }
    
    this.activeTab = tabIndex;
  }

  getVisibleTabs(): any[] {
    const currentUser = this.authService.getCurrentUser();
    if (!currentUser || currentUser.role !== 'admin') {
      // Non-admin users see only tabs 0 and 1
      return [this.tabs[0], this.tabs[1]];
    }
    // Admin users see all tabs
    return this.tabs;
  }

  isCurrentUserAdmin(): boolean {
    const currentUser = this.authService.getCurrentUser();
    return currentUser && currentUser.role === 'admin';
  }

  toggleCreateUserForm(): void {
    this.showCreateUserForm = !this.showCreateUserForm;
    if (!this.showCreateUserForm) {
      this.resetCreateUserForm();
    }
  }

  resetCreateUserForm(): void {
    const currentUser = this.authService.getCurrentUser();
    
    this.newUserForm = {
      login: '',
      password: '',
      firstName: '',
      lastName: '',
      email: '',
      phone: '',
      permissionNumber: '',
      role: (currentUser && currentUser.login !== 'admin') ? 'user' : 'user'
    };
  }

  canSelectRoleWhenCreating(): boolean {
    const currentUser = this.authService.getCurrentUser();
    // Only master admin can select role when creating users
    return currentUser && currentUser.login === 'admin';
  }

  createUser(): void {
    if (!this.newUserForm.login || !this.newUserForm.password || !this.newUserForm.firstName) {
      this.notificationService.error('Login, hasło i imię są wymagane');
      return;
    }

    this.creatingUser = true;

    const newUser: CreateUserRequest = {
      login: this.newUserForm.login,
      password: this.newUserForm.password,
      firstName: this.newUserForm.firstName,
      lastName: this.newUserForm.lastName,
      email: this.newUserForm.email,
      phone: this.newUserForm.phone,
      permissionNumber: this.newUserForm.permissionNumber,
      role: this.newUserForm.role
    };

    this.userService.createUser(newUser).subscribe(
      (response) => {
        console.log('[Settings] User created:', response);
        this.notificationService.success('Użytkownik został pomyślnie utworzony!');
        this.creatingUser = false;
        this.resetCreateUserForm();
        this.showCreateUserForm = false;

        // Reload users list
        this.loadUsers();
      },
      (error: any) => {
        console.error('[Settings] Error creating user:', error);
        const errorMsg = error.error?.error || error.error?.message || 'Błąd podczas tworzenia użytkownika';
        this.notificationService.error(errorMsg);
        this.creatingUser = false;
      }
    );
  }

  editUser(user: UserDTO): void {
    window.dispatchEvent(new CustomEvent('editUserEvent', { detail: user }));
  }

  canDeleteUser(user: UserDTO): boolean {
    const currentUser = this.authService.getCurrentUser();
    if (!currentUser) return false;

    // Cannot delete self
    if (currentUser.userId === user.userId) return false;

    // Only admins can delete users
    if (currentUser.role !== 'admin') return false;

    // Master admin (login = admin) can delete anyone
    if (currentUser.login === 'admin') return true;

    // Regular admin can only delete regular users (not admins)
    return user.role !== 'admin';
  }

  openDeleteConfirmDialog(user: UserDTO): void {
    // Ask for password confirmation before deleting
    this.userToConfirm = user;
    this.confirmDialogAction = 'delete';
    this.showPasswordConfirmDialog = true;
  }

  confirmPasswordAction(): void {
    if (!this.confirmDialogPassword || !this.userToConfirm) {
      return;
    }

    this.confirmDialogError = '';
    this.confirmingAction = true;

    this.userService.deleteUser(this.userToConfirm.userId, this.confirmDialogPassword).subscribe(
      (response) => {
        console.log('[Settings] User deleted:', response);
        this.showPasswordConfirmDialog = false;
        this.confirmingAction = false;
        this.confirmDialogPassword = '';
        this.userToConfirm = null;

        // Reload users list
        this.loadUsers();
        this.notificationService.success('Użytkownik został pomyślnie usunięty!');
      },
      (error: any) => {
        console.error('[Settings] Error deleting user:', error);
        const errorMsg = error.error?.message || error.error?.error || 'Błąd podczas usuwania użytkownika';
        this.notificationService.error(errorMsg);
      }
    );
  }

  cancelPasswordConfirmDialog(): void {
    this.showPasswordConfirmDialog = false;
    this.confirmDialogPassword = '';
    this.confirmDialogError = '';
    this.userToConfirm = null;
  }

  canChangeUserRole(user: UserDTO): boolean {
    const currentUser = this.authService.getCurrentUser();
    if (!currentUser) return false;

    // Cannot change own role
    if (currentUser.userId === user.userId) return false;

    // Only master admin (login = admin) can change roles
    // Regular admin cannot change roles of existing users
    if (currentUser.login !== 'admin') return false;

    return true;
  }

  onRoleChanged(user: UserDTO): void {
    const currentUser = this.authService.getCurrentUser();
    
    // Validation - only master admin can change roles
    if (!currentUser || currentUser.login !== 'admin') {
      // Reload user to reset the role
      this.loadUsers();
      this.notificationService.error('Tylko master administrator może zmieniać role użytkowników');
      return;
    }

    // Validation - check if user can make this change
    if (!this.canChangeUserRole(user)) {
      // Reload user to reset the role
      this.loadUsers();
      this.notificationService.error('Brak uprawnień do zmiany tej roli');
      return;
    }

    this.updatingRoleUserId = user.userId;

    this.userService.updateUserRole(user.userId, user.role).subscribe(
      (response) => {
        console.log('[Settings] User role updated:', response);
        this.notificationService.success(`Rola użytkownika ${user.login} została zmieniona`);
        this.updatingRoleUserId = null;
      },
      (error: any) => {
        console.error('[Settings] Error updating user role:', error);
        const errorMsg = error.error?.message || error.error?.error || 'Błąd podczas zmiany roli użytkownika';
        this.notificationService.error(errorMsg);
        this.updatingRoleUserId = null;
        // Reload users to revert the role change
        this.loadUsers();
      }
    );
  }

  getSafeHtml(html: string): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(html);
  }
}
