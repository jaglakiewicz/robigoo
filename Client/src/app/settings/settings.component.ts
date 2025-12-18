import { Component, OnInit } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { UserService, CreateUserRequest, UserDTO } from '../services/user.service';

@Component({
  selector: 'app-settings',
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.css']
})
export class SettingsComponent implements OnInit {
  activeTab = 0;
  private editUserListener: any;

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
  createUserError = '';
  createUserSuccess = false;
  creatingUser = false;

  // Password confirmation dialog
  showPasswordConfirmDialog = false;
  confirmDialogAction: 'delete' | 'create' = 'delete';
  userToConfirm: UserDTO | null = null;
  confirmDialogPassword = '';
  confirmDialogError = '';
  confirmingAction = false;
  deletingUserId: number | null = null;

  constructor(
    private authService: AuthService,
    private userService: UserService
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

    const updateData = {
      firstName: this.userSettings.firstName,
      lastName: this.userSettings.lastName,
      email: this.userSettings.email,
      phone: this.userSettings.phone,
      permissionNumber: this.userSettings.permissionNumber,
      language: this.userSettings.language,
      theme: this.userSettings.theme
    };

    this.userService.updateProfile(updateData).subscribe(
      (response) => {
        console.log('[Settings] Profile updated:', response);
        this.userSettingsSuccess = true;
        this.savingSettings = false;
        setTimeout(() => { this.userSettingsSuccess = false; }, 3000);

        // Update current user in auth service
        const currentUser = this.authService.getCurrentUser();
        if (currentUser) {
          currentUser.firstName = this.userSettings.firstName;
          currentUser.lastName = this.userSettings.lastName;
          currentUser.email = this.userSettings.email;
          currentUser.phone = this.userSettings.phone;
          currentUser.permissionNumber = this.userSettings.permissionNumber;
          localStorage.setItem('currentUser', JSON.stringify(currentUser));
        }
      },
      (error: any) => {
        console.error('[Settings] Error saving settings:', error);
        this.userSettingsError = error.error?.message || 'Błąd podczas zapisywania ustawień';
        this.savingSettings = false;
      }
    );
  }

  changePassword(): void {
    if (this.passwordForm.newPassword !== this.passwordForm.confirmPassword) {
      this.changePasswordError = 'Hasła nie pasują do siebie';
      return;
    }

    if (this.passwordForm.newPassword.length < 6) {
      this.changePasswordError = 'Hasło musi mieć co najmniej 6 znaków';
      return;
    }

    this.changePasswordError = '';
    this.changePasswordSuccess = false;
    this.changingPassword = true;

    this.userService.changePassword({
      oldPassword: this.passwordForm.oldPassword,
      newPassword: this.passwordForm.newPassword
    }).subscribe(
      (response) => {
        console.log('[Settings] Password changed:', response);
        this.changePasswordSuccess = true;
        this.changingPassword = false;
        this.passwordForm = { oldPassword: '', newPassword: '', confirmPassword: '' };
        this.showPasswordForm = false;
        setTimeout(() => { this.changePasswordSuccess = false; }, 3000);
      },
      (error: any) => {
        console.error('[Settings] Error changing password:', error);
        this.changePasswordError = error.error?.message || 'Błąd podczas zmiany hasła';
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
    this.activeTab = tabIndex;
  }

  toggleCreateUserForm(): void {
    this.showCreateUserForm = !this.showCreateUserForm;
    if (!this.showCreateUserForm) {
      this.resetCreateUserForm();
    }
  }

  resetCreateUserForm(): void {
    this.newUserForm = {
      login: '',
      password: '',
      firstName: '',
      lastName: '',
      email: '',
      phone: '',
      permissionNumber: '',
      role: 'user'
    };
    this.createUserError = '';
  }

  createUser(): void {
    if (!this.newUserForm.login || !this.newUserForm.password || !this.newUserForm.firstName) {
      this.createUserError = 'Login, hasło i imię są wymagane';
      return;
    }

    this.createUserError = '';
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
        this.createUserSuccess = true;
        this.creatingUser = false;
        this.resetCreateUserForm();
        this.showCreateUserForm = false;
        setTimeout(() => { this.createUserSuccess = false; }, 3000);

        // Reload users list
        this.loadUsers();
      },
      (error: any) => {
        console.error('[Settings] Error creating user:', error);
        this.createUserError = error.error?.error || error.error?.message || 'Błąd podczas tworzenia użytkownika';
        this.creatingUser = false;
      }
    );
  }

  editUser(user: UserDTO): void {
    window.dispatchEvent(new CustomEvent('editUserEvent', { detail: user }));
  }

  canDeleteUser(user: UserDTO): boolean {
    // Cannot delete admin users
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
        this.createUserSuccess = true;
        setTimeout(() => { this.createUserSuccess = false; }, 3000);
      },
      (error: any) => {
        console.error('[Settings] Error deleting user:', error);
        this.confirmDialogError = error.error?.message || error.error?.error || 'Błąd podczas usuwania użytkownika';
      }
    );
  }

  cancelPasswordConfirmDialog(): void {
    this.showPasswordConfirmDialog = false;
    this.confirmDialogPassword = '';
    this.confirmDialogError = '';
    this.userToConfirm = null;
  }
}
