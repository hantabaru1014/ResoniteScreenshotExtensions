export type float3 = [number, number, number];
export type floatQ = [number, number, number, number];

export type MetadataUser = {
  id: string;
  name?: string;
  machineId: string;
};

export type MetadataUserInfo = {
  user: MetadataUser;
  isInVR: boolean;
  isPresent: boolean;
  headPosition: float3;
  headOrientation: floatQ;
  sessionJoinTimestamp: Date;
};

export enum StereoLayout {
  None,
  Horizontal_LR,
  Vertical_LR,
  Horizontal_RL,
  Vertical_RL,
  Custom,
}

export enum SessionAccessLevel {
  Private,
  LAN,
  Contacts,
  ContactsPlus,
  RegisteredUsers,
  Anyone,
}

export type Metadata = {
  locationName: string;
  locationURL?: string;
  locationHost: MetadataUser;
  locationAccessLevel?: SessionAccessLevel;
  locationHiddenFromListing?: boolean;
  timeTaken: Date;
  takenBy: MetadataUser;
  takenGlobalPosition: float3;
  takenGlobalRotation: floatQ;
  takenGlobalScale: float3;
  appVersion: string;
  userInfos: MetadataUserInfo[];
  cameraManufacturer: string;
  cameraModel: string;
  cameraFOV: number;
  is360: boolean;
  stereoLayout: StereoLayout;
};
